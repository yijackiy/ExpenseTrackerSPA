namespace ExpenseTracker

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Sitelets
open WebSharper.Charting

// ---------------------------------------------------------------------------
// Template binding
// ---------------------------------------------------------------------------
type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

// ---------------------------------------------------------------------------
// Router endpoints
// ---------------------------------------------------------------------------
type EndPoint =
    | [<EndPoint "/">]            Dashboard
    | [<EndPoint "/transactions">] Transactions
    | [<EndPoint "/reports">]     Reports

// ---------------------------------------------------------------------------
// Domain model
// ---------------------------------------------------------------------------
[<JavaScript>]
module Model =

    type Category = string

    type EntryType = Income | Expense

    type Entry = {
        Id       : int
        Date     : string
        Category : Category
        Note     : string
        Amount   : float
        Kind     : EntryType
    }

    let mutable private nextId = 1
    let newId () =
        let id = nextId
        nextId <- nextId + 1
        id

    let defaultEntries : Entry list = [
        { Id = newId(); Date = "2026-04-01"; Category = "Salary";        Note = "Monthly pay";       Amount = 3500.00; Kind = Income  }
        { Id = newId(); Date = "2026-04-03"; Category = "Rent";          Note = "April rent";        Amount = 1200.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-05"; Category = "Groceries";     Note = "Weekly shop";       Amount =  180.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-08"; Category = "Freelance";     Note = "Web project";       Amount =  850.00; Kind = Income  }
        { Id = newId(); Date = "2026-04-10"; Category = "Transport";     Note = "Monthly pass";      Amount =   72.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-12"; Category = "Dining";        Note = "Dinner out";        Amount =   55.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-14"; Category = "Utilities";     Note = "Electric bill";     Amount =   95.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-16"; Category = "Entertainment"; Note = "Streaming subs";    Amount =   30.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-18"; Category = "Health";        Note = "Gym membership";    Amount =   45.00; Kind = Expense }
        { Id = newId(); Date = "2026-04-20"; Category = "Gift";          Note = "Birthday present";  Amount =  200.00; Kind = Income  }
    ]

    let entries = Var.Create defaultEntries

// ---------------------------------------------------------------------------
// Chart helpers
// ---------------------------------------------------------------------------
[<JavaScript>]
module Charts =

    open Model

    let pieChart (pairs: (string * float) list) =
        let chart = Chart.Pie pairs
        Renderers.ChartJs.Render(chart, Size = Size(280, 280))

    let barChart (pairs: (string * float) list) =
        let chart = Chart.Bar pairs
        Renderers.ChartJs.Render(chart, Size = Size(560, 260))

// ---------------------------------------------------------------------------
// Pages
// ---------------------------------------------------------------------------
[<JavaScript>]
module Pages =

    open Model

    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------
    let fmt (v: float) = sprintf "%.2f" v

    let totalOf kind (lst: Entry list) =
        lst |> List.filter (fun e -> e.Kind = kind) |> List.sumBy (fun e -> e.Amount)

    let balanceOf (lst: Entry list) =
        totalOf Income lst - totalOf Expense lst

    // -----------------------------------------------------------------------
    // Dashboard page
    // -----------------------------------------------------------------------
    let DashboardPage () =

        let summary =
            entries.View |> View.Map (fun lst ->
                let inc  = totalOf Income  lst
                let exp  = totalOf Expense lst
                let bal  = inc - exp
                inc, exp, bal
            )

        let incomeView  = summary |> View.Map (fun (i,_,_) -> fmt i)
        let expenseView = summary |> View.Map (fun (_,e,_) -> fmt e)
        let balanceView = summary |> View.Map (fun (_,_,b) -> fmt b)

        let balanceClass =
            summary |> View.Map (fun (_,_,b) ->
                if b >= 0.0 then "positive" else "negative"
            )

        // Recent 5 entries
        let recentView =
            entries.View |> View.Map (fun lst ->
                lst
                |> List.sortByDescending (fun e -> e.Date)
                |> List.truncate 5
            )

        // Overview pie chart (income vs expense)
        let overviewChart =
            summary |> View.Map (fun (i, e, _) ->
                Charts.pieChart [ "Income", i; "Expenses", e ]
            )

        IndexTemplate.Dashboard()
            .TotalIncome(incomeView)
            .TotalExpense(expenseView)
            .Balance(balanceView)
            .BalanceClass(balanceClass)
            .RecentRows(
                recentView.DocSeqCached(fun (entry: Entry) ->
                    let kindLabel = if entry.Kind = Income then "Income" else "Expense"
                    let kindClass = if entry.Kind = Income then "badge badge-income" else "badge badge-expense"
                    let sign = if entry.Kind = Income then "+" else "-"
                    IndexTemplate.RecentRow()
                        .EntryDate(entry.Date)
                        .EntryCategory(entry.Category)
                        .EntryNote(entry.Note)
                        .EntryKind(kindLabel)
                        .EntryKindClass(kindClass)
                        .EntrySign(sign)
                        .EntryAmount(fmt entry.Amount)
                        .Doc()
                )
            )
            .OverviewChart(overviewChart.V)
            .Doc()

    // -----------------------------------------------------------------------
    // Transactions page
    // -----------------------------------------------------------------------
    let TransactionsPage () =

        let newCategory = Var.Create "Salary"
        let newNote     = Var.Create ""
        let newAmount   = Var.Create 0.00
        let newKind     = Var.Create "income"
        let newDate     = Var.Create ""

        let addEntry () =
            if newNote.Value <> "" && newAmount.Value > 0.0 then
                let kind = if newKind.Value = "income" then Income else Expense
                let entry = {
                    Id       = Model.newId()
                    Date     = if newDate.Value = "" then "2026-04-21" else newDate.Value
                    Category = newCategory.Value
                    Note     = newNote.Value
                    Amount   = newAmount.Value
                    Kind     = kind
                }
                entries.Value <- entries.Value @ [entry]
                newNote.Value   <- ""
                newAmount.Value <- 0.00
            else
                JS.Alert "Please fill in all fields."

        let allRows =
            entries.View.DocSeqCached(fun (entry: Entry) ->
                let kindLabel = if entry.Kind = Income then "Income" else "Expense"
                let kindClass = if entry.Kind = Income then "badge badge-income" else "badge badge-expense"
                let sign      = if entry.Kind = Income then "+" else "-"
                IndexTemplate.TransactionRow()
                    .RowDate(entry.Date)
                    .RowCategory(entry.Category)
                    .RowNote(entry.Note)
                    .RowKind(kindLabel)
                    .RowKindClass(kindClass)
                    .RowSign(sign)
                    .RowAmount(fmt entry.Amount)
                    .RemoveEntry(fun _ ->
                        entries.Value <- entries.Value |> List.filter (fun e -> e.Id <> entry.Id)
                    )
                    .Doc()
            )

        IndexTemplate.Transactions()
            .NewCategory(newCategory)
            .NewNote(newNote)
            .NewAmount(newAmount)
            .NewKind(newKind)
            .NewDate(newDate)
            .AddEntry(fun _ -> addEntry())
            .AllRows(allRows)
            .Doc()

    // -----------------------------------------------------------------------
    // Reports page
    // -----------------------------------------------------------------------
    let ReportsPage () =

        // Expense breakdown by category (pie)
        let expensePieChart =
            entries.View |> View.Map (fun lst ->
                let groups =
                    lst
                    |> List.filter (fun e -> e.Kind = Expense)
                    |> List.groupBy (fun e -> e.Category)
                    |> List.map (fun (cat, es) -> cat, es |> List.sumBy (fun e -> e.Amount))
                if groups.IsEmpty then
                    Charts.pieChart [ "No data", 1.0 ]
                else
                    Charts.pieChart groups
            )

        // Income breakdown by category (pie)
        let incomePieChart =
            entries.View |> View.Map (fun lst ->
                let groups =
                    lst
                    |> List.filter (fun e -> e.Kind = Income)
                    |> List.groupBy (fun e -> e.Category)
                    |> List.map (fun (cat, es) -> cat, es |> List.sumBy (fun e -> e.Amount))
                if groups.IsEmpty then
                    Charts.pieChart [ "No data", 1.0 ]
                else
                    Charts.pieChart groups
            )

        // Monthly summary bar chart (income vs expense per day label)
        let barChartView =
            entries.View |> View.Map (fun lst ->
                let incMap =
                    lst |> List.filter (fun e -> e.Kind = Income)
                        |> List.groupBy (fun e -> e.Category)
                        |> List.map (fun (c, es) -> c, es |> List.sumBy (fun e -> e.Amount))
                if incMap.IsEmpty then
                    Charts.barChart [ "No data", 0.0 ]
                else
                    Charts.barChart incMap
            )

        // Summary stats
        let statsView =
            entries.View |> View.Map (fun lst ->
                let inc = totalOf Income  lst
                let exp = totalOf Expense lst
                let bal = inc - exp
                fmt inc, fmt exp, fmt bal
            )

        let totalIncReport  = statsView |> View.Map (fun (i,_,_) -> i)
        let totalExpReport  = statsView |> View.Map (fun (_,e,_) -> e)
        let netReport       = statsView |> View.Map (fun (_,_,b) -> b)

        IndexTemplate.Reports()
            .ExpensePieChart(expensePieChart.V)
            .IncomePieChart(incomePieChart.V)
            .BarChart(barChartView.V)
            .ReportIncome(totalIncReport)
            .ReportExpense(totalExpReport)
            .ReportNet(netReport)
            .Doc()

// ---------------------------------------------------------------------------
// Sidebar controller (replaces inline JS in index.html)
// ---------------------------------------------------------------------------
[<JavaScript>]
module SidebarController =

    open WebSharper.JavaScript

    let run () =
        let sidebar   = JS.Document.GetElementById "sidebar"
        let overlay   = JS.Document.GetElementById "sidebar-overlay"
        let hamburger = JS.Document.GetElementById "hamburger-btn"

        let openSidebar ()  =
            sidebar.ClassList.Add "open"
            overlay.ClassList.Add "active"

        let closeSidebar () =
            sidebar.ClassList.Remove "open"
            overlay.ClassList.Remove "active"

        hamburger.AddEventListener("click", fun (_: Dom.Event) ->
            if sidebar.ClassList.Contains "open" then closeSidebar()
            else openSidebar()
        )

        overlay.AddEventListener("click", fun (_: Dom.Event) -> closeSidebar())

        let setActiveNav () =
            let hash =
                let h = JS.Window.Location.Hash
                if h = "" then "#/" else h
            let links = JS.Document.QuerySelectorAll ".sidebar-nav a"
            for i = 0 to links.Length - 1 do
                let a = links.Item(i) :?> Dom.Element
                let href = a.GetAttribute "href"
                if href = hash || (hash = "#/" && href = "/#/") then
                    a.ClassList.Add "active"
                else
                    a.ClassList.Remove "active"

        JS.Window.AddEventListener("hashchange", fun (_: Dom.Event) -> setActiveNav())
        JS.Window.AddEventListener("load",       fun (_: Dom.Event) -> setActiveNav())
        setActiveNav()

// ---------------------------------------------------------------------------
// Client entry point
// ---------------------------------------------------------------------------
[<JavaScript>]
module Client =

    let router = Router.Infer<EndPoint>()
    let currentPage = Router.InstallHash Dashboard router

    [<SPAEntryPoint>]
    let Main =
        let renderPage (page: Var<EndPoint>) =
            page.View.Map(fun ep ->
                match ep with
                | Dashboard    -> Pages.DashboardPage()
                | Transactions -> Pages.TransactionsPage()
                | Reports      -> Pages.ReportsPage()
            )
            |> Doc.EmbedView

        IndexTemplate()
            .PageContent(renderPage currentPage)
            .Bind()

        SidebarController.run()
