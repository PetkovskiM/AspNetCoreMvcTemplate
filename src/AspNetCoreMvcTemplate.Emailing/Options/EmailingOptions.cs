using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.Options
{
    public class EmailingOptions
    {
        public const string SectionName = "Emailing";

        public bool Enabled { get; set; } = true;

        public string Provider { get; set; } = "Logging";

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
    }
}
