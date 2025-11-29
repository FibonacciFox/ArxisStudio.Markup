using System.Collections.Generic;

namespace JsonUiEditor.Models
{
    public class RootModel
    {
        public string FormName { get; set; } = "Form";
        public string NamespaceSuffix { get; set; } = "Forms";
        public string ParentClassType { get; set; } = "Avalonia.Controls.UserControl";
        public Dictionary<string, object>? Properties { get; set; }
    }
}