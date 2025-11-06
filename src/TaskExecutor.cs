using System;
using Zenith.Error;
using Zenith.Models;

namespace Zenith.Executor
{
    public class TaskExecutor
    {
        private Queue<TaskModel> tasksQueue = new Queue<TaskModel>();
        public TaskfileModel taskfileModel { get; set; } = new();

        public void ResolveDependencies(string taskName)
        {
            if (string.IsNullOrEmpty(taskName))
            {
                throw new UserInputError("Task name cannot be empty!");
            }

            TaskModel mainTask = taskfileModel.Tasks[FindTaskModelIndex(taskName)];
            tasksQueue.Enqueue(mainTask);

            List<string> dependencies = mainTask.Dependencies;

            if (!string.Equals(dependencies[0], "null"))
            {
                foreach (string dep in dependencies)
                {
                    (bool, int) isDuplicate = CheckDuplicateTasks(dep);
                    if (isDuplicate.Item1)
                    {
                        throw new SyntaxError("Found more than one task with the same name", isDuplicate.Item2);
                    }

                    if (!TaskQueueContains(dep))
                    {
                        ResolveDependencies(dep);
                    }
                    else
                    {
                        throw new SyntaxError("Infinite loop detected", mainTask.LineNumber);
                    }
                }
            }
        }

        private bool TaskQueueContains(string name)
        {
            foreach (TaskModel task in tasksQueue)
            {
                if (task.Name == name)
                {
                    return true;
                }
            }
            
            return false;
        }

        private (bool, int) CheckDuplicateTasks(string name)
        {
            int count = 0;

            foreach (TaskModel task in taskfileModel.Tasks)
            {
                if (count > 1) return (true, task.LineNumber);

                if (task.Name == name)
                {
                    count++;
                }
            }

            return (false, 0);
        }

        private int FindTaskModelIndex(string name)
        {
            (bool, int) isDuplicate = CheckDuplicateTasks(name);
            if (isDuplicate.Item1)
            {
                throw new SyntaxError("Found more than one task with the same name", isDuplicate.Item2);
            }

            for (int i = 0; i < taskfileModel.Tasks.Count; i++)
            {
                if (taskfileModel.Tasks[i].Name == name)
                {
                    return i;
                }
            }

            throw new UserInputError($"No task called {name} was found!");
        }

        public void PrintQueue()
        {
            Console.WriteLine("=====Queue=====");
            foreach (TaskModel task in tasksQueue)
            {
                task.PrintModel();
            }
        }
    }
}
