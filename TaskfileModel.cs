using System;

namespace Zenith.Models
{
    public class TaskfileModel
    {
        public List<VariableModel> Variables { get; set; } = new();
        public List<TaskModel> Tasks { get; set; } = new();
    }

    public class VariableModel
    {
        required public string Name { get; set; }
        required public string Value { get; set; }
        required public int LineNumber { get; set; }
    }

    public class TaskModel
    {
        required public string Name { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public List<string> Commands { get; set; } = new();
        required public int LineNumber { get; set; }
    }
}
