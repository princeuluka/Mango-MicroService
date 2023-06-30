using coco.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coco.Services
{
    public class TransactionService :ITransactionService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TransactionService> _logger;
        public TransactionService(AppDbContext db, ILogger<TransactionService>logger)
        {
            _db = db;
            _logger = _logger;
        }

        public double Deposit()
        {
            
            Console.WriteLine("Please input your account number");
            int accountNumber = int.Parse(Console.ReadLine());
            Console.WriteLine("Please input the amount you would like to deposit");
            double depositAmount = double.Parse(Console.ReadLine());
            
            var data = _db.CustomerDetails.AsNoTracking().FirstOrDefault(n => n.CustomerId == accountNumber);
            data.AccountBalance = data.AccountBalance+ depositAmount;
            _db.CustomerDetails.Entry(data).State = EntityState.Detached;
            CustomerDetails newData = new()
            {
                AccountBalance = data.AccountBalance,
                AccountType = data.AccountType,
                CustomerId =  data.CustomerId,
                FirstName = data.FirstName,
                LastName = data.LastName
            };
            _db.ChangeTracker.Clear();
            _db.Entry(newData).State = EntityState.Modified;
            _db.Set<CustomerDetails>().Update(newData); 
             _db.SaveChanges();
            return data.AccountBalance;
        }

        public bool Transfer()
        {
            Console.WriteLine("Please Input sender account number");
            int senderAccountNumber = int.Parse(Console.ReadLine());
            Console.WriteLine("Please Input beneficiary account number");
            int beneficiaryAccountNumber = int.Parse(Console.ReadLine());
            Console.WriteLine("Please Input transfer amount");
            double transferAmount = double.Parse(Console.ReadLine());

            var SenderDetails = _db.CustomerDetails.AsNoTracking().FirstOrDefault(n=>n.CustomerId == senderAccountNumber);
            var benfiiaryDetails = _db.CustomerDetails.AsNoTracking().FirstOrDefault(n=>n.CustomerId == beneficiaryAccountNumber);

            if (SenderDetails.AccountBalance > transferAmount )
            {
                SenderDetails.AccountBalance = SenderDetails.AccountBalance - transferAmount;
                benfiiaryDetails.AccountBalance = benfiiaryDetails.AccountBalance + transferAmount;

                _db.ChangeTracker.Clear();
                _db.CustomerDetails.Update(SenderDetails);
                _db.CustomerDetails.Update(benfiiaryDetails);
                _db.SaveChanges();
                return true;
            }
            _logger.LogError("Unable to transfer -- Insufficent balance");
            return false;
        }

        public (bool,double) Withdrawal()
        {
            Console.WriteLine("Please input your account number");
            int accountNumber = int.Parse(Console.ReadLine());
            Console.WriteLine("Please input the amount you would like to withdraw");
            double withdrawAmount = double.Parse(Console.ReadLine());
            var data = _db.CustomerDetails.AsNoTracking().FirstOrDefault(n => n.CustomerId == accountNumber);

            if (data.AccountBalance > withdrawAmount)
            {
                if (data.AccountType == "savings" && data.AccountBalance < 1000.0)
                {
                    _logger.LogError("Unable to withdraw -- Minimun Account Balance Threshold");
                    return (false,0.0);
                }
                else
                {
                    data.AccountBalance = data.AccountBalance - withdrawAmount;
                    _db.ChangeTracker.Clear();
                    _db.CustomerDetails.Update(data);
                    _db.SaveChanges();
                    return (true,data.AccountBalance);

                }
            }
            else
            {
                 _logger.LogError("Unable to transfer -- Insufficent balance");
                return (false, 0.0);
            }

            
        }
    }
}
