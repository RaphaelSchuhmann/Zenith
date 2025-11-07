# Zenith <img src="assets/Zenith_Logo.png" width=30>

**Zenith** is a cross-platform, console-basd automation tool written in **C# (.NET 9)**.
It executes and manages custom-defined tasks from a simple text configuration file - **Taskfile.txt** - similar to `make`, `just`, or `ninja`, but with a more declarative
syntax and first-class Windows/Linux support.

---

## Features

### Task Definitions
Define tasks in a `Taskfile.txt`;
```txt
task build: clean
    csc -out:bin/app.exe src/*.cs
```    
Each task:
- Has a **name**
- Can depend on other tasks
- Executes one or more **shell commands**

---

### Variables

Use variables for reusability and clarity:
```txt
set CC = gcc
set OUT = bin

task compile:
    ${CC} -o ${OUT}/app main.c
```

- Declared with `set <NAME> = <VALUE>`
- Referenced as `${NAME}`
- Exanded before command execution

---

### Dependencies

Tasks can depend on others:
```txt
task all: clean, build, test
```
- Tasks run in correct dependency order
- Circular dependencies are detected and reported

---

### CLI Usage

```bash
task run <taskname>     Alias for above
task list               List all available task and dependencies
task --dry-run <task>   Print what would be executed without running
task --help             Show usage information
task --version          Display version and environment details
```
Example:
```bash
task run all
```

---

### Environment Variables
Each task inherits the enviornment and includes built-ins:
| Variable      | Description                                       |
| ------------- | ------------------------------------------------- |
| `${TASK_NAME}`| Current task name                                 |
| `${TASK_ROOT}`| Directory where Taskfile resides                  |
| `${OS}`       | windows / linux / macos                           |
| `${PWD}`      | Current working directory                         |

---

### Error Handling
- Non-zero exit codes stop execution
- Circular dependencies and syntax errors are reported clearly and stop execution
- Missing or invalid task definitions cause fatal error

Example:
```
[compile] gcc: command not found
[error] Task 'compile' failed with exist code 123
```

---
### Example Taskfile.txt
```txt
# Global variables
set SRC = src
set OUT = bin
set CC = gcc

# Define tasks
task clean:
    echo Cleaning...
    rm -rf ${OUT}

task compile: clean
    mkdir -p ${OUT}
    ${CC} -o ${OUT}/app ${SRC}/*.c

task run: compile
    ${OUT}/app

task all: clean, compile, run
```

Running `task all` executes:
clean → compile → run → all

---

### Example Output
Default:
```
> task all
[clean] rm -rf bin
[compile] mkdir -p bin
[compile] gcc -o bin/app src/*.c
[run] bin/app
[done] all tasks completed successfully.
```

Verbose mode:

```
[clean] -> started (0.002s)
[compile] -> started (0.113s)
[compile] -> finished (OK)
```

---

## Design Constraints
| Constraint      | Description                                       |
| --------------- | ------------------------------------------------- |
| Environment     | Runs on **.NET 8+**                               |
| Encoding        | UTF-8                                             |
| Taskfile Format | Simple, line-based syntax (no JSON/YAML)          |
| Portability     | Native Windows + Linux support                    |
| Dependencies    | No external binaries required beyond system shell |
| Concurrency     | Future support for parallel execution             |

---
## Planned Features

- Incremental builds: Skip up-to-date tasks
- Parallel execution: Run independent tasks concurrently
- Logging: Write structured logs to task.log
- Aliases: e.g. alias b = build
- Built-in utilities: copy, delete, echo, etc.

---
## Building from Source
Prerequisites
- [.NET 8+ SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git

Build
```bash
git clone https://github.com/RaphaelSchuhmann/Zenith.git
cd Zenith
dotnet build
```
Run
```bash
dotnet run --project Tasker -- <args>
```
Or after publishing:
```bash
task <taskname>
```
---
## Development
Code style:
- Follows .NET SDK-style project layout
- Structured with clear separation between parser, executor, and CLI interface
- Comprehensive error messages with colored console output

---
License
This project is released under the **MIT License**.
See [LICENSE](LICENSE.txt) for details.

---
## Author
Developed by [Raphael Schuhmann](https://github.com/RaphaelSchuhmann)
