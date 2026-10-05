# Student Lab: Student Records with ASP.NET Core MVC

Build a small web application that stores student records. By the end of this lab you will have created an MVC project, connected it to a SQLite database with Entity Framework Core, and implemented list, details, create, edit, and delete.

Work in your own folder. The `src/StudentRecords` project in the lecture repository is the finished reference. Do not type this lab inside that folder.

Each student record has:

- First name
- Last name
- Course
- Grade, a number from 0 to 100

## Before you start

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). Then open a terminal and run:

```powershell
dotnet --version
```

A `10.x` version means you can continue. If the command is not recognized, close the terminal, reopen it, and try again after the SDK install finishes.

You can use Visual Studio, Visual Studio Code, or any editor. The steps below use the `dotnet` command so they work in every editor.

Create a folder for your work and move into it:

```powershell
mkdir StudentRecordsLab
cd StudentRecordsLab
```

## Part 1 — Create the MVC project

Create a new Model-View-Controller web app that targets .NET 10:

```powershell
dotnet new mvc -n StudentRecords -f net10.0
cd StudentRecords
dotnet run
```

The console prints addresses such as `https://localhost:7087` and `http://localhost:5065`. The numbers can differ on your machine. Open the HTTPS address in a browser. Trust the development certificate if the browser asks. You can also trust it from the terminal:

```powershell
dotnet dev-certs https --trust
```

Stop the app with `Ctrl+C` before you edit files.

### Checkpoint 1

The browser shows the template home page titled **Welcome**. You have a project that compiles and serves Razor views. Leave the terminal in the `StudentRecords` project folder for the rest of the lab.

If you use Visual Studio instead of the CLI: **File > New > Project > ASP.NET Core Web App (Model-View-Controller)**, name it `StudentRecords`, and choose **.NET 10**.

## Part 2 — Map the project

Open the project and find these pieces. You will change some of them in later parts.

| Path | What it does |
| --- | --- |
| `Program.cs` | Starts the app and registers services such as MVC |
| `Controllers/HomeController.cs` | Handles the home page. A public method here is an action |
| `Views/Home/Index.cshtml` | The HTML for the home action |
| `Views/Shared/_Layout.cshtml` | The shared navbar and footer around every page |
| `Models/` | C# classes that describe data. Validation attributes go here |
| `appsettings.json` | Settings, including the database connection string |
| `wwwroot/` | CSS, JavaScript, and other files the browser downloads |

A request for `/Home/Index` follows this path:

1. Routing in `Program.cs` chooses `HomeController` and the `Index` action.
2. The action returns `View()`.
3. Razor looks for `Views/Home/Index.cshtml`, wraps it in `_Layout.cshtml`, and sends HTML to the browser.

The student pages will follow the same path: `StudentsController` and `Views/Students/`.

## Part 3 — Add the database

SQLite stores the database in one file named `students.db`. You do not install a database server.

### 3.1 Add the Entity Framework packages

From the project folder:

```powershell
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

`Sqlite` is the database provider. `Design` supplies the tools that create migrations. The project file will list a version such as `10.0.12`. Use whatever `10.x` version `dotnet add package` writes. Keep the EF tool in the next step on that same major version.

### 3.2 Connection string

Open `appsettings.json` and add a `ConnectionStrings` section above `Logging`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=students.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`Data Source=students.db` means the database file is created in the project folder when you apply the migration.

### 3.3 The Student model

Create `Models/Student.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace StudentRecords.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Course { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Grade")]
    public decimal Grade { get; set; }
}
```

Why these attributes matter:

- `[Required]` rejects a missing name or course.
- `[StringLength]` limits how much text can be stored.
- `[Display(Name = "...")]` is the label Razor shows in forms and tables.
- `[Range(0, 100)]` rejects a grade below 0 or above 100.
- `Id` has no attributes. Entity Framework treats an integer property named `Id` as the primary key and generates its value.

### 3.4 The database context

Create a `Data` folder and `Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StudentRecords.Models;

namespace StudentRecords.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
}
```

`DbSet<Student> Students` is the `Students` table. The constructor receives options so `Program.cs` can choose SQLite and the connection string.

### 3.5 Register the context

At the top of `Program.cs`, add:

```csharp
using Microsoft.EntityFrameworkCore;
using StudentRecords.Data;
```

After `AddControllersWithViews()`, register the database:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

The rest of `Program.cs` stays as the template created it. ASP.NET Core will pass `AppDbContext` into a controller constructor when you ask for it. That is dependency injection.

### 3.6 Create and apply the migration

Install the EF Core command-line tool once. Match the major version to the package you added. If the package version is `10.0.12`, run:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

If the tool is already installed at another version, update it:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.12
```

Close and reopen the terminal if `dotnet ef` is still not recognized. Then, from the project folder:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

The first command writes a `Migrations` folder. That folder is a C# description of the `Students` table. The second command creates `students.db` and the table. Columns are `Id`, `FirstName`, `LastName`, `Course`, and `Grade`.

### Checkpoint 2

- `Migrations` contains a file whose name ends in `_InitialCreate.cs`
- `students.db` exists in the project folder
- `dotnet build` succeeds

Do not commit `students.db`. It is local data. Commit the `Migrations` folder so another machine can rebuild the same table.

## Part 4 — The students controller

Create `Controllers/StudentsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentRecords.Data;
using StudentRecords.Models;

namespace StudentRecords.Controllers;

public class StudentsController : Controller
{
    private readonly AppDbContext _context;

    public StudentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _context.Students
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();

        return View(students);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student)
    {
        if (!ModelState.IsValid)
        {
            return View(student);
        }

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student student)
    {
        if (id != student.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(student);
        }

        _context.Update(student);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is not null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
```

Read it action by action:

- **Index** loads every student, sorted by last name, and passes the list to the view.
- **Details, Edit, and Delete** look up one student by `id`. A missing id or a missing row returns HTTP 404.
- **Create (GET)** shows an empty form. **Create (POST)** runs only when the browser submits the form.
- `[HttpPost]` marks the method that receives the form. Without it, MVC would treat both methods as GET and the names would conflict.
- `[ValidateAntiForgeryToken]` checks a hidden token in the form so another site cannot submit the form for the user.
- `ModelState.IsValid` is false when a data annotation fails. The action redisplays the form instead of saving.
- `SaveChangesAsync` writes the insert, update, or delete to SQLite.
- `RedirectToAction(nameof(Index))` sends the browser to the list with a GET request. That pattern is Post-Redirect-Get. Refreshing the list does not submit the form again.
- The delete POST method is named `DeleteConfirmed` so it does not clash with the GET method. `ActionName("Delete")` still maps the form post to this method.

Database calls use `async`/`await` because they wait on disk. The thread can serve other requests while SQLite works.

There is a code generator, `dotnet aspnet-codegenerator`, that can write a controller and views for you. Type them by hand in this lab so you can see each action. Use the generator later when the pattern is familiar.

## Part 5 — Razor views

Create a `Views/Students` folder and the five files below. Tag helpers such as `asp-for` and `asp-action` connect the HTML to the `Student` model and to controller actions. `_ViewImports.cshtml` already enables them.

### `Views/Students/Index.cshtml`

```cshtml
@model IEnumerable<StudentRecords.Models.Student>

@{
    ViewData["Title"] = "Students";
}

<h1>Students</h1>

<p>
    <a class="btn btn-primary" asp-action="Create">Create New</a>
</p>

@if (!Model.Any())
{
    <p>No students yet. Create the first record.</p>
}

<table class="table">
    <thead>
        <tr>
            <th>@Html.DisplayNameFor(model => model.FirstName)</th>
            <th>@Html.DisplayNameFor(model => model.LastName)</th>
            <th>@Html.DisplayNameFor(model => model.Course)</th>
            <th>@Html.DisplayNameFor(model => model.Grade)</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var student in Model)
        {
            <tr>
                <td>@Html.DisplayFor(modelItem => student.FirstName)</td>
                <td>@Html.DisplayFor(modelItem => student.LastName)</td>
                <td>@Html.DisplayFor(modelItem => student.Course)</td>
                <td>@Html.DisplayFor(modelItem => student.Grade)</td>
                <td>
                    <a asp-action="Details" asp-route-id="@student.Id">Details</a> |
                    <a asp-action="Edit" asp-route-id="@student.Id">Edit</a> |
                    <a asp-action="Delete" asp-route-id="@student.Id">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

`DisplayNameFor` uses the `[Display]` names from the model, so the column heading is **First Name** rather than `FirstName`.

### `Views/Students/Details.cshtml`

```cshtml
@model StudentRecords.Models.Student

@{
    ViewData["Title"] = "Student Details";
}

<h1>Student Details</h1>

<dl class="row">
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.FirstName)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.FirstName)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.LastName)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.LastName)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.Course)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.Course)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.Grade)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.Grade)</dd>
</dl>

<div>
    <a class="btn btn-primary" asp-action="Edit" asp-route-id="@Model.Id">Edit</a>
    <a class="btn btn-secondary" asp-action="Index">Back to list</a>
</div>
```

### `Views/Students/Create.cshtml`

```cshtml
@model StudentRecords.Models.Student

@{
    ViewData["Title"] = "Create Student";
}

<h1>Create Student</h1>

<div class="row">
    <div class="col-md-6">
        <form asp-action="Create">
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            <div class="mb-3">
                <label asp-for="FirstName" class="form-label"></label>
                <input asp-for="FirstName" class="form-control" />
                <span asp-validation-for="FirstName" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="LastName" class="form-label"></label>
                <input asp-for="LastName" class="form-control" />
                <span asp-validation-for="LastName" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Course" class="form-label"></label>
                <input asp-for="Course" class="form-control" />
                <span asp-validation-for="Course" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Grade" class="form-label"></label>
                <input asp-for="Grade" class="form-control" />
                <span asp-validation-for="Grade" class="text-danger"></span>
            </div>
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Back to list</a>
        </form>
    </div>
</div>

@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }
}
```

`asp-for` binds each input to a property. The form tag helper adds the anti-forgery token. The scripts section turns on browser-side checks so an empty name or a grade of 150 fails before the request is sent. The controller still checks `ModelState` because browser checks can be skipped.

### `Views/Students/Edit.cshtml`

```cshtml
@model StudentRecords.Models.Student

@{
    ViewData["Title"] = "Edit Student";
}

<h1>Edit Student</h1>

<div class="row">
    <div class="col-md-6">
        <form asp-action="Edit">
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            <input type="hidden" asp-for="Id" />
            <div class="mb-3">
                <label asp-for="FirstName" class="form-label"></label>
                <input asp-for="FirstName" class="form-control" />
                <span asp-validation-for="FirstName" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="LastName" class="form-label"></label>
                <input asp-for="LastName" class="form-control" />
                <span asp-validation-for="LastName" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Course" class="form-label"></label>
                <input asp-for="Course" class="form-control" />
                <span asp-validation-for="Course" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Grade" class="form-label"></label>
                <input asp-for="Grade" class="form-control" />
                <span asp-validation-for="Grade" class="text-danger"></span>
            </div>
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Back to list</a>
        </form>
    </div>
</div>

@section Scripts {
    @{
        await Html.RenderPartialAsync("_ValidationScriptsPartial");
    }
}
```

The hidden `Id` field must be posted with the form. Without it, the update would not know which row to change.

### `Views/Students/Delete.cshtml`

```cshtml
@model StudentRecords.Models.Student

@{
    ViewData["Title"] = "Delete Student";
}

<h1>Delete Student</h1>
<h3>Are you sure you want to delete this student?</h3>

<dl class="row">
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.FirstName)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.FirstName)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.LastName)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.LastName)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.Course)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.Course)</dd>
    <dt class="col-sm-2">@Html.DisplayNameFor(model => model.Grade)</dt>
    <dd class="col-sm-10">@Html.DisplayFor(model => model.Grade)</dd>
</dl>

<form asp-action="Delete">
    <input type="hidden" asp-for="Id" />
    <button type="submit" class="btn btn-danger">Delete</button>
    <a asp-action="Index" class="btn btn-secondary">Back to list</a>
</form>
```

The GET action only shows the confirmation. The row is removed when this form is submitted.

### Home page and navbar

Replace the contents of `Views/Home/Index.cshtml` so the home page links to the list:

```cshtml
@{
    ViewData["Title"] = "Home";
}

<div class="text-center">
    <h1 class="display-4">Student Records</h1>
    <p class="lead">A lecture sample for ASP.NET Core MVC, Entity Framework Core, and SQLite.</p>
    <a class="btn btn-primary" asp-controller="Students" asp-action="Index">Open student list</a>
</div>
```

In `Views/Shared/_Layout.cshtml`, add a Students link next to Home:

```cshtml
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Students" asp-action="Index">Students</a>
</li>
```

## Part 6 — Try the application

Run it again from the project folder:

```powershell
dotnet run
```

Open the site and use **Students** or **Open student list**.

### Checkpoint 3 — Create

1. The list says **No students yet**.
2. Choose **Create New**.
3. Save a student, for example first name `Ana`, last name `Cruz`, course `Application Development`, grade `92`.
4. The browser returns to the list and shows that row. The address bar is the list URL, not `/Students/Create`. That is Post-Redirect-Get.

### Checkpoint 4 — Validation

1. Create another student and leave the first name blank. The page stays on the form and explains that the field is required.
2. Enter a grade of `150`. The page rejects it because of `[Range(0, 100)]`.
3. Fix the values and save. The list shows two students, ordered by last name.

### Checkpoint 5 — Details, edit, delete

1. **Details** shows one student and does not change the database.
2. **Edit** changes the course or grade. After Save, the list shows the new values.
3. **Delete** asks you to confirm, then removes the row.

Try `/Students/Details/999` when no student has that id. The response is 404.

## Troubleshooting

**`dotnet` is not recognized.** Install the .NET 10 SDK and open a new terminal.

**`dotnet ef` is not recognized.** Install or update the global tool (`dotnet tool install --global dotnet-ef --version 10.0.12`), then open a new terminal.

**The startup project does not target the EF tools, or the tool version does not match.** The `dotnet-ef` major version must match `Microsoft.EntityFrameworkCore.Design` in the project file. Update the tool to that `10.x` version.

**`SQLite Error` or `no such table: Students`.** The migration was not applied. From the project folder run `dotnet ef database update`.

**The browser warns that the certificate is not trusted.** Run `dotnet dev-certs https --trust`, then reload.

**Validation messages never appear in the browser.** Confirm the Create and Edit views include the `Scripts` section that renders `_ValidationScriptsPartial`. The controller still rejects bad data even when those scripts are missing.

**The page is 404 for `/Students`.** The controller class must be named `StudentsController`, the views must live in `Views/Students`, and the app must have been restarted after the files were added.

**Port already in use.** Another `dotnet run` is still active. Stop it with `Ctrl+C`, or run `dotnet run --launch-profile http` and use the HTTP address the console prints.
