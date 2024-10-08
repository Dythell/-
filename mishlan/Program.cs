var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
var app = builder.Build();
app.UseCors("AllowAll");
app.UseRouting();


List<Reservation> bd = new List<Reservation>()
{
    new Reservation(1, "сергей", "шило","петрович",921219212,"ничего","Пушкина",52,2024,10,8,"Завтрак","Олег"),
    new Reservation(2, "михаил", "шило","петрович",92121955,"ничего","Пушкина",52,2024,10,8,"Завтрак","Олег"),
};




app.MapGet("/", () => bd);

app.MapPost("/", (Reservation res) =>
{
    int newId = bd.Any() ? bd.Max(r => r.Number) + 1 : 1;
    res.Number = newId;
    bd.Add(res);
    return Results.Json(res);
});

app.MapPut("/{number}", (int number, ReservationUpdate upd) =>
{
    Reservation space = bd.Find(res => res.Number == number);
    if (space == null)
        return Results.NotFound("нету");

    if (upd.DepartureDate.HasValue)
    {
        if (space.DepartureDate != upd.DepartureDate.Value)
        {
            Console.WriteLine($"Изменение даты выезда для резервации {space.Number}: " +
                              $"С {space.DepartureDate} на {upd.DepartureDate.Value}");
        }
        space.DepartureDate = upd.DepartureDate.Value;
    }

    if (!space.DepartureDate.HasValue)
    {
        space.DepartureDate = DateTime.Now;
        Console.WriteLine($"Выселение резервации {space.Number} на {space.DepartureDate}");
        SendNotification(space);
    }

    if (upd.CheckindateDate.HasValue)
    {
        space.CheckindateDate = upd.CheckindateDate.Value;
    }

    if (!string.IsNullOrEmpty(upd.Wishes))
        space.Wishes = upd.Wishes;
    if (!string.IsNullOrEmpty(upd.Necessity))
        space.Necessity = upd.Necessity;
    if (!string.IsNullOrEmpty(upd.Admin))
        space.Admin = upd.Admin;

    return Results.Json(space);
});

app.MapGet("/{number}", (int number) =>
{
    Reservation reservation = bd.Find(res => res.Number == number);
    if (reservation == null)
        return Results.NotFound("неть");

    return Results.Json(reservation);
});

app.MapGet("/search", (string? wishes, string? admin, string? FullName) =>
{
    var filteredReservation = bd.Where(res =>
        (string.IsNullOrEmpty(wishes) || res.Wishes.Equals(wishes, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrEmpty(admin) || res.Admin.Equals(admin, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrEmpty(FullName) || res.FullName.Equals(FullName, StringComparison.OrdinalIgnoreCase))
    ).ToList();
    if (filteredReservation.Count == 0)
        return Results.NotFound("неть");

    return Results.Json(filteredReservation);
});

app.MapGet("/stat/avg-residence-time", () =>
{
    var completedOrders = bd.Where(ord => ord.DepartureDate.HasValue).ToList();

    if (completedOrders.Count == 0)
        return Results.Json(new { AverageTimeInHours = 0, CompletionTimes = new List<object>() });

    var completionTimes = completedOrders.Select(ord => new
    {
        ord.Number,
        TimeTakenInHours = (int)((ord.DepartureDate.Value - ord.CheckindateDate).TotalMinutes / 60)
    }).ToList();

    int totalHours = completionTimes.Sum(ct => ct.TimeTakenInHours);
    int averageTimeInHours = totalHours / completedOrders.Count;
    return Results.Json(new
    {
        AverageTimeInHours = averageTimeInHours,
        CompletionTimes = completionTimes
    });
});

app.MapGet("/stat/booking-frequency", () =>
{
    var bookingFrequency = bd.GroupBy(res => res.Apartnumber)
        .Select(group => new
        {
            ApartmentNumber = group.Key,
            BookingCount = group.Count()
        })
        .ToList();

    if (bookingFrequency.Count == 0)
        return Results.NotFound("Нет данных о заселении");

    return Results.Json(bookingFrequency);
});

app.MapGet("/stat/completed-reservations", () =>
{
    var currentDate = DateTime.Now;
    var completedReservations = bd.Where(res => res.DepartureDate.HasValue && res.DepartureDate.Value < currentDate).ToList();

    if (completedReservations.Count == 0)
        return Results.NotFound("Нет завершенных периодов заселения");

    return Results.Json(completedReservations);
});



app.Run();
void SendNotification(Reservation reservation)
{
    Console.WriteLine($"Уведомление: Резервация {reservation.Number} завершена. Дата выселения: {reservation.DepartureDate}");
}






class Reservation
{
    int number;
    string name;
    string surname;
    string lastname;
    int phonenumber;
    string wishes;
    string adress;
    int apartnumber;
    int day;
    int month;
    int year;
    public DateTime CheckindateDate { get; set; }
    public DateTime? DepartureDate{ get; set; }
    string necessity;
    string admin;
    public string FullName => $"{Surname} {Name} {Lastname}";

    public Reservation(int number, string name, string surname, string lastname, int phonenumber, string wishes, string adress, int apartnumber, int year, int month, int day, string necessity, string admin)
    {
        Number = number;
        Name = name;
        Surname = surname;
        Lastname = lastname;
        Phonenumber = phonenumber;
        Wishes = wishes;
        Adress = adress;
        Apartnumber = apartnumber;

        Year = year;
        Month = month;
        Day = day;
        CheckindateDate = new DateTime(year, month, day);
        DepartureDate = null;
        Necessity = necessity;
        Admin = admin;

    }

    public int Number { get => number; set => number = value; }
    public string Name { get => name; set => name = value; }
    public string Surname { get => surname; set => surname = value; }
    public string Lastname { get => lastname; set => lastname = value; }
    public int Phonenumber { get => phonenumber; set => phonenumber = value; }
    public string Wishes { get => wishes; set => wishes = value; }
    public string Adress { get => adress; set => adress = value; }
    public int Apartnumber { get => apartnumber; set => apartnumber = value; }
    public int Day { get => day; set => day = value; }
    public int Month { get => month; set => month = value; }
    public int Year { get => year; set => year = value; }
    public string Necessity { get => necessity; set => necessity = value; }
    public string Admin { get => admin; set => admin = value; }

}

public class ReservationUpdate
{
    public DateTime? DepartureDate { get; set; }
    public string Wishes { get; set; }
    public string Necessity { get; set; }
    public string Admin { get; set; }
    public  DateTime? CheckindateDate { get; set; }
}