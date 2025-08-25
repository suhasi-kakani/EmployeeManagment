namespace EmployeeManagment.Models
{

    public class Admin : User
    {
        public Admin()
        {
            RoleString = nameof(Role.Admin); 
        }
    }
}
