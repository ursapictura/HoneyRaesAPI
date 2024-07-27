// List<HoneyRaesAPI.Models.Customer> customers = new List<HoneyRaesAPI.Models.Customer> {};
// List<HoneyRaesAPI.Models.Employee> employees = new List<HoneyRaesAPI.Models.Employee> {};
// List<HoneyRaesAPI.Models.ServiceTicket> serviceTickets = new List<HoneyRaesAPI.Models.ServiceTicket> {};

// This using directive imports all of the names of HoneyRaesAPI.Models into this file
// so that we can refer to them without the whole name. Now you should be able to create 
// your collections like this:

using HoneyRaesAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Win32.SafeHandles;
List<Customer> customers = new List<Customer> {
    new Customer { 
        Id = 1,
        Name = "Frank Talik",
        Address = "74 Wingham Drive, Crossville, TN",
    },

    new Customer {
        Id = 2,
        Name = "Lorie Gantz",
        Address = "315 Moss Ridge, Nameless, TN"
    },

     new Customer {
        Id = 3,
        Name = "Zeb Whitman",
        Address = "4759 Fox Wood Drive, Ensor, TN"
     }
 };

List<Employee> employees = new List<Employee> {
    new Employee {
        Id = 1,
        Name = "Garrett Harmen",
        Specialty = "Inventory"
    },

    new Employee {
        Id = 2,
        Name = "Rebecca Buendia",
        Specialty = "Hospitality"
    }
 };

List<ServiceTicket> serviceTickets = new List<ServiceTicket> { 
    new ServiceTicket {
        Id = 1,
        CustomerId = 1,
        Description = "Dispute a charge",
        Emergency = false,
    },

    new ServiceTicket {
        Id = 2,
        CustomerId = 2,
        EmployeeId = 1,
        Description = "Return broken items",
        Emergency = false,
        DateCompleted = new DateTime(2024, 05, 20),
    }, 

    new ServiceTicket { 
        Id = 3,
        CustomerId = 3,
        EmployeeId = 2,
        Description = "Injured by recalled item",
        Emergency = true,
    },

    new ServiceTicket {
        Id = 4,
        CustomerId = 1,
        EmployeeId = 1,
        Description = "New charges to dispute. Suspects fraud",
        Emergency = true,
    },

    new ServiceTicket {
        Id = 5,
        CustomerId = 2,
        EmployeeId = 2,
        Description = "Wants broken items back",
        Emergency = false,
        DateCompleted = new DateTime(2023, 07, 20),
    }


};


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    // return serviceTickets.FirstOrDefault(st => st.Id == id);
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(t => t.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    serviceTicket.Customer = customers.FirstOrDefault(c => c.Id == serviceTicket.CustomerId);
    return Results.Ok(serviceTicket);
});

app.MapGet("/servicetickets/emergency", () =>
{
   return serviceTickets.Where(st => st.Emergency).Where(st => st.DateCompleted == null); 
});

app.MapGet("/servicetickets/unassigned", () => 
{
    return serviceTickets.Where(st => st.EmployeeId == null);
});

app.MapGet("/servicetickets/completed", () => 
{
    return serviceTickets.Where(st => st.DateCompleted != null)
                         .OrderBy(st => st.DateCompleted);
});

app.MapGet("/sevicetickets/priority", () => 
{
    return serviceTickets.Where(st => st.DateCompleted == null)
                        .OrderByDescending(st => st.Emergency)
                        .ThenByDescending(st => st.EmployeeId);
});

app.MapPost("/servicetickets", (ServiceTicket serviceTicket) =>
{
    // creates a new id (When we get to it later, our SQL database will do this for us like JSON Server did!)
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/servicetickets/{id}", (int id) =>
{
    serviceTickets.Remove(serviceTickets.FirstOrDefault(st => st.Id == id));
});

app.MapPut("/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    // Find serviceTicket with Id matching id input.
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id); 

    // find the index of ticketToUpdate in the serviceTickets List.
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate); 

    // if no ticket is found with an Id matching the input, return Not Found.
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }

    //the id in the request route doesn't match the id from the ticket in the request body. That's a bad request!
    if (id != serviceTicket.Id)
    {
        return Results.BadRequest();

    }
    //the serviceTicket being PUT should overwrite the current serviceTicket at the matching index in the List.
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapPost("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    ticketToComplete.DateCompleted = DateTime.Today;
});

app.MapGet("/employees", () => 
{
    return employees;
});

app.MapGet("/employees/{id}", (int id) => 
{
    // return employees.FirstOrDefault(e => e.Id == id);
    Employee employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee == null)
    {
        return Results.NotFound();
    }
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    return Results.Ok(employee);

});

app.MapGet("/employees/available", () => 
{
    var incompleteTickets = serviceTickets
                            .Where(st => st.DateCompleted == null)
                            .Select(st => st.EmployeeId)
                            .Distinct()
                            .ToList();

    return employees.Where(e => !incompleteTickets.Contains(e.Id));
});

app.MapGet("/employees/{id}/customers", (int id) =>
{
    var employee = employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
    {
        return Results.NotFound();
    }
    var employeeTickets = serviceTickets
                        .Where(st => st.EmployeeId == id)
                        .Select(st => st.CustomerId)
                        .Distinct()
                        .ToList();
    var employeeCustomers = customers.Where(c => employeeTickets.Contains(c.Id));
    return Results.Ok(employeeCustomers);
});

app.MapGet("/employees/ofthemonth", () => 
{
    var ticketsCompletedInMonth = serviceTickets.Where(st => st.DateCompleted >= DateTime.Now.AddMonths(-1) && st.EmployeeId != null);

    if (ticketsCompletedInMonth.Count() <= 0)
    {
        return Results.NotFound();
    }

    var employeeTickets = ticketsCompletedInMonth
                            .GroupBy(t => t.EmployeeId)
                            .Select(group => new
                            {
                                EmployeeId = group.Key,
                                TicketCount = group.Count()
                            })
                            .OrderByDescending(e => e.TicketCount)
                            .FirstOrDefault();
    
    var employeeOfTheMonth = employees.FirstOrDefault(e => e.Id == employeeTickets.EmployeeId);
    return Results.Ok(employeeOfTheMonth);
});

app.MapGet("/customers", () => 
{
    return customers;
});

app.MapGet("/customers/{id}", (int id) => 
{
    // return customers.FirstOrDefault(c => c.Id == id);
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId== id).ToList();
    return Results.Ok(customer);
});

app.MapGet("/customers/inactive", () => 
{
 var recentCompleteTickets = serviceTickets
                            .Where(st => st.DateCompleted >= DateTime.Now.AddYears(-1))
                            .Select(st => st.CustomerId)
                            .Distinct()
                            .ToList();

 return customers.Where(c => !recentCompleteTickets.Contains(c.Id));
});

app.Run();