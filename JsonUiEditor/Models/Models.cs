using System.Collections.Generic;

namespace JsonUiEditor.Models
{
    public class ControlModel
    {
        public string? Type { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }
}