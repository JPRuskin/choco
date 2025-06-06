// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.licensing;
using chocolatey.infrastructure.logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("license", "Manipulate the Chocolatey License", Version = "2.5.0")]
    public class ChocolateyLicenseCommand : ChocolateyCommandBase, ICommand
    {
        private static readonly Regex _licenseCountRegex = new Regex(@"\[.*?(?<licensedMachineCount>\d+).*?\]");

        public void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                     "Name - the name of the package. Required with some actions. Defaults to empty.",
                     option => configuration.PinCommand.Name = option.UnquoteSafe())
                ;
        }

        public void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single license command must be listed. Please see the help menu for those commands");
            }

            var command = LicenseCommandType.Unknown;
            var unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);

            if (command == LicenseCommandType.Unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand))
                {
                    this.Log().Warn("Unknown command {0}. Setting to info.".FormatWith(unparsedCommand));
                }

                command = LicenseCommandType.Info;
            }

            configuration.LicenseCommand.Command = command;

            // First non-switch argument that is not a name=value pair will be treated as the license to test or install.
            configuration.Input = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault(arg => !arg.StartsWith("-") && !arg.ContainsSafe("="));
        }

        public void Validate(ChocolateyConfiguration configuration)
        {
            if (configuration.Input != string.Empty)
            {
                // Test the existence of the file
            }
        }

        public bool MayRequireAdminAccess(ChocolateyConfiguration config)
        {
            var requiresAdmin = false;
            switch (config.LicenseCommand.Command)
            {
                case LicenseCommandType.Info:
                    requiresAdmin = false;
                case LicenseCommandType.Set:
                    requiresAdmin = true;
            }
            return requiresAdmin;
        }

        public void DryRun(ChocolateyConfiguration configuration)
        {
            Run(configuration);
        }

        public void Run(ChocolateyConfiguration config)
        {
            switch (config.LicenseCommand.Command)
            {
                case LicenseCommandType.Info:
                    GetLicense(config);
                    break;
                case LicenseCommandType.Set:
                    SetLicense(config);
                    break;
            }
        }

        private void GetLicense(ChocolateyConfiguration config)
        {
            var ourLicense = new ChocolateyLicense();
            if (config.Input != string.Empty)
            {
                ourLicense = LicenseValidation.Validate(config.Input);
            }
            else
            {
                ourLicense = LicenseValidation.Validate();
            }
            var logger = config.RegularOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly;

            if (ourLicense.LicenseType == ChocolateyLicenseType.Foss || ourLicense.LicenseType == ChocolateyLicenseType.Unknown)
            {
                this.Log().Warn(logger, "No Commercial License Found, running with Open Source License.");
                return;
            }

            if (!ourLicense.IsValid)
            {
                this.Log().Warn(logger, "We have found an invalid license for Chocolatey {0}: {1}", ourLicense.LicenseType, ourLicense.InvalidReason);
                Environment.ExitCode = 1;
                return;
            }

            var nodeCount = ParseLicenseStringForLicenseCount(ourLicense.Name);

            if (config.RegularOutput)
            {
                this.Log().Info("Registered to:   {0}".FormatWith(ourLicense.Name));
                this.Log().Info("Expiration Date: {0}".FormatWith(ourLicense.ExpirationDate?.ToString("dd MMMM yyyy ")));
                this.Log().Info("License type:    {0}".FormatWith(ourLicense.LicenseType));
                this.Log().Info("Node Count:      {0}".FormatWith(nodeCount));
            }
            else
            {
                // Headers: Name, LicenseType, ExpirationDate, NodeCount
                this.Log().Info("{0}|{1}|{2}|{3}".FormatWith(ourLicense.Name, ourLicense.LicenseType, ourLicense.ExpirationDate?.ToString("yyyy-MM-dd"), nodeCount));
            }
        }

        private void SetLicense(ChocolateyConfiguration config)
        {
            var licensePath = config.Input;
            var newLicense = LicenseValidation.Validate(licensePath);

            var logger = config.RegularOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly;

            if (newLicense.IsValid)
            {
                this.Log().Info(logger, "Installing the new license for Chocolatey {0}: {1}", newLicense.LicenseType, licensePath);
                System.IO.File.Copy(licensePath, ApplicationParameters.LicenseFileLocation, true);
            }
            else
            {
                this.Log().Warn(logger, "The provided license is an invalid license for Chocolatey {0}: {1}", newLicense.LicenseType, newLicense.InvalidReason);
                Environment.ExitCode = 1;
            }
        }

        private int ParseLicenseStringForLicenseCount(string licenseName)
        {
            if (string.IsNullOrEmpty(licenseName))
            {
                return 0;
            }

            // Use Regex to find the licensed machine count
            var match = _licenseCountRegex.Match(licenseName);

            if (match.Success && int.TryParse(match.Groups["licensedMachineCount"].Value, out var licenseCount))
            {
                return licenseCount;
            }

            return 0;
        }

        protected override string GetCommandDescription(CommandForAttribute attribute, ChocolateyConfiguration configuration)
        {
            return @"Show information about the current Chocolatey CLI license.";
        }

        protected override IEnumerable<string> GetCommandExamples(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[]
            {
                "choco license",
                "choco license info"
            };
        }

        protected override IEnumerable<string> GetCommandUsage(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[]
            {
                "choco license [info] [<options/switches>]",
            };
        }

#pragma warning disable IDE0022, IDE1006

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

#pragma warning restore IDE0022, IDE1006
    }
}
