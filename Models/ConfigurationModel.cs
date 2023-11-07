namespace ML.CMS.Models
{
    [LocalizedDisplay("Plugins.ML.CMS.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*EnableMiniProfilerInPublicStore")]
        public bool EnableMiniProfilerInPublicStore { get; set; }

        [UIHint("Textarea"), AdditionalMetadata("rows", 2)]
        [LocalizedDisplay("*MiniProfilerIgnorePaths")]
        public string MiniProfilerIgnorePaths { get; set; }

        [LocalizedDisplay("*DisplayWidgetZones")]
        public bool DisplayWidgetZones { get; set; }

        [LocalizedDisplay("*DisplayMachineName")]
        public bool DisplayMachineName { get; set; }
    }
}
