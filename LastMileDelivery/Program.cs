using LastMileDelivery.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Database Connection
var strConn = builder.Configuration.GetSection("ConnectionString")["DefaultConnection"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlServer(strConn));


// code to talk to other websites
builder.Services.AddHttpClient();

// tells the app that you will be using Controllers to handle logic and Views to show the screens
builder.Services.AddControllersWithViews();
//Find my Minimal API endpoints and understand them.
builder.Services.AddEndpointsApiExplorer();

//test all your API endpoints easily
builder.Services.AddSwaggerGen();

// Required for Session state
builder.Services.AddDistributedMemoryCache(); 
//Session Management (Memory)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// creates the app 
var app = builder.Build();

// HST are like airbags for your website. They protect it from hackers and bad actors.
// Handling expections and errors
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

//show images, CSS, and JavaScript files
app.UseStaticFiles();

//It looks at the URL and figures out which controller to send the user to.
app.UseRouting();

//It keeps track of who is logged in and their role (Customer, Agent, Admin)
app.UseSession();

//It checks if the user has permission to see a page
app.UseAuthorization();

//Default Route (The Front Door)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//starts the web server
app.Run();