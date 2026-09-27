using KnightCore.Application;
using KnightCore.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services,IConfiguration config)
    {
        var db=services.GetRequiredService<AppDbContext>();
        if(!await db.Segments.AnyAsync())
        {
            var entries=new[]{
                ("restaurants","Restaurants","utensils","Restaurant Suite","From the first order to the final bill. Keep your restaurant running in sync.",24000m,"sage",new[]{"Menu & table management","Order tracking","Billing & daily reports","Staff access"},new[]{("inventory","Inventory control","Track ingredients, stock and reorder levels.",6000m),("online","Online ordering","Accept orders through your own branded storefront.",9000m),("loyalty","Customer loyalty","Reward regulars with points and tailored offers.",4500m)}),
                ("salons","Salons & spas","scissors","Salon Studio","A calmer calendar. Happier clients. A complete workspace for your salon.",18000m,"rose",new[]{"Appointment calendar","Services & pricing","Customer directory","Billing & receipts"},new[]{("memberships","Membership plans","Create recurring packages and prepaid memberships.",5000m),("commissions","Staff commissions","Keep team earnings and payouts organized.",4000m),("reminders","Appointment reminders","Send timely appointment notifications.",3000m)}),
                ("property","Property owners","building","Property Desk","Bring every building, tenant and rent record into one clear view.",28000m,"blue",new[]{"Buildings & units","Tenant records","Rent ledger","Owner dashboard"},new[]{("complaints","Service requests","Track maintenance requests from opening to resolution.",5500m),("collections","Collection integration","Connect a supported payment collection provider.",8000m),("reports","Portfolio analytics","Compare occupancy, rent and performance.",6000m)}),
                ("clinics","Clinics","heart","Clinic Flow","Less paperwork between appointments. More time for your patients.",26000m,"violet",new[]{"Appointment scheduling","Patient registration","Billing & receipts","Role-based staff access"},new[]{("inventory","Medical inventory","Track supplies, purchase records and stock levels.",6500m),("reminders","Patient reminders","Send appointment notifications to patients.",3500m),("reporting","Operational reports","Understand visits and clinic activity.",4500m)}),
                ("coaching","Coaching institutes","graduation","Learning Hub","Connect your batches, students and daily institute operations.",22000m,"amber",new[]{"Student directory","Batch management","Attendance tracking","Fee records"},new[]{("tests","Online assessments","Create and manage objective tests.",7000m),("parents","Parent portal","Give parents a view of progress and attendance.",5000m),("integrations","Payment integration","Connect a supported payment provider.",6500m)}),
                ("manufacturing","Manufacturers","factory","Operations Core","A clear picture of your orders, products and shop-floor priorities.",35000m,"slate",new[]{"Product catalog","Sales orders","Basic stock records","Operations dashboard"},new[]{("procurement","Procurement","Manage purchase requests and supplier orders.",8500m),("production","Production planning","Plan production runs and monitor progress.",12000m),("analytics","Operational analytics","Track lead times, stock and order performance.",6500m)})
            };
            var sort=0;
            foreach(var e in entries)
            {
                db.Segments.Add(new(){Id=e.Item1,Name=e.Item2,Icon=e.Item3,SortOrder=sort++});
                var p=new SoftwarePackage{Id=e.Item1,SegmentId=e.Item1,Name=e.Item4,Summary=e.Item5,Accent=e.Item7};
                var v=new PackageVersion{Id=e.Item1+"-v1",PackageId=p.Id,Version=1,BasePrice=e.Item6,IncludedJson=Json.Write(e.Item8),Delivery="2–3 weeks"};
                foreach(var f in e.Item9)v.Features.Add(new(){Id=e.Item1+"-"+f.Item1+"-v1",PackageVersionId=v.Id,Code=f.Item1,Name=f.Item2,Description=f.Item3,UnitPrice=f.Item4});
                v.Features.Add(new(){Id=e.Item1+"-branches-v1",PackageVersionId=v.Id,Code="branches",Name="Additional locations",Description="Extend your workspace to another branch or location.",PriceMode="PerUnit",UnitPrice=3500m,MaxQuantity=20});
                p.Versions.Add(v);db.Packages.Add(p);
            }
            db.InvoiceSeries.Add(new(){Year=DateTime.UtcNow.Year});
            await db.SaveChangesAsync();
        }
        var roles=services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach(var role in new[]{"Customer","Admin"})if(!await roles.RoleExistsAsync(role))await roles.CreateAsync(new(role));
        var email=config["Bootstrap:AdminEmail"];var password=config["Bootstrap:AdminPassword"];
        if(!string.IsNullOrWhiteSpace(email)&&!string.IsNullOrWhiteSpace(password))
        {
            var users=services.GetRequiredService<UserManager<AppUser>>();
            var admin=await users.FindByEmailAsync(email);
            if(admin is null)
            {
                admin=new(){UserName=email,Email=email,DisplayName="KnightCore Admin",EmailConfirmed=true};
                var created=await users.CreateAsync(admin,password);
                if(!created.Succeeded)throw new InvalidOperationException("Admin bootstrap failed: "+string.Join("; ",created.Errors.Select(x=>x.Description)));
                await users.AddToRoleAsync(admin,"Admin");
            }
            else if(!await users.IsInRoleAsync(admin,"Admin"))
                throw new InvalidOperationException("Bootstrap address belongs to an existing customer. Choose a separate administrator address.");
        }
    }
}

