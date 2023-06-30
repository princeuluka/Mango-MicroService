using coco.SD;
using coco.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coco.Services
{
    public class AccountService:IAccountService
    {
 
        private readonly AppDbContext _db;
        public AccountService(AppDbContext db)
        {
           
            _db = db;

        }

        public dynamic CheckBalance()
        {
            Console.WriteLine("Please Input account number");
            int accountId = int.Parse(Console.ReadLine());
            var data = _db.CustomerDetails.AsNoTracking().FirstOrDefault(u => u.CustomerId == accountId);
                if (data!=null)
                {
                    return data.AccountBalance;
                }
                else
                {
                    return "Incorrect Account Number or Account does not exist";
                }
           
        }

        public int CreateAccount()
        {
            CustomerDetails customer = new();
            
            Console.WriteLine("Please Input First Name");
            customer.FirstName = Console.ReadLine();
            if (char.IsLower(customer.FirstName,0))
            {
                Console.WriteLine("Please First Name should start with a capital letter");
                Console.WriteLine("Please Input First Name");
                customer.FirstName = Console.ReadLine();
            }

            Console.WriteLine("Please Input Last Name");
            customer.LastName = Console.ReadLine();
            if (char.IsLower(customer.LastName, 0))
            {
                Console.WriteLine("Please Last Name should start with a capital letter");
                Console.WriteLine("Please Input Last Name");
                customer.LastName = Console.ReadLine();
            }

            Console.WriteLine("Please Select Account Type : Input '1' for savings and '2' for current");
            string accountType = Console.ReadLine();
            switch (accountType)
            {
                case "1":
                    customer.AccountType = "Savings";
                    break;
                case "2":
                    customer.AccountType = "Current";
                    break;
                default:
                    customer.AccountType = "Savings";
                    break;
            }


            Random rand = new Random();

            customer.CustomerId = rand.Next(100000, 199999);
            customer.AccountBalance = 0.0;

            _db.CustomerDetails.Add(customer);
            _db.SaveChanges();
           
            return (customer.CustomerId);
        }

        public string ShowAll()
        {
            List<CustomerDetails> customerDetails = _db.CustomerDetails.ToList();

            string table = 
                "|--------------------|--------------------|--------------------|--------------------|";
            string tableHeader =
                "|FULLNAME            | ACCOUNT NUMBER     | ACCOUNT TYPE       | BALANCE            |";
            string data =
                 "|FULL_NAME          | ACCOUNT_NUMBER     | ACCOUNT_TYPE       | BankBALANCE        |";
            string Close = 
                "**************************************************************************************";

            Console.WriteLine(table);
            Console.WriteLine(tableHeader);

            foreach (CustomerDetails customer in customerDetails)
            {
                Console.WriteLine(data.Replace("FULL_NAME", customer.FirstName + " " + customer.LastName)
                    .Replace("ACCOUNT_NUMBER",customer.CustomerId.ToString())
                    .Replace("ACCOUNT_TYPE",customer.AccountType)
                    .Replace("BankBALANCE",customer.AccountBalance.ToString()));
            }


            return "Done";
        }
    }
}
