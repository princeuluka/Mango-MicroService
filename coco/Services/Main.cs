using coco.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coco.Services
{
    public class Main:IMain
    {
        private readonly IAccountService _accountService;
        private readonly ITransactionService _transactionService;
        public Main(IAccountService accountService,ITransactionService transactionService)
        {
            _accountService = accountService;
            _transactionService = transactionService;
        }
        public void Run()
        {
            Console.WriteLine();
            Console.WriteLine("Welcome to COCO Banking");
            Console.WriteLine("Please Select an option below");
            Console.WriteLine("1. Open an accont");
            Console.WriteLine("2. Withdrawal");
            Console.WriteLine("3. Deposit");
            Console.WriteLine("4. Transfer");
            Console.WriteLine("5. Check Balance");
            Console.WriteLine("6. Show All Accounts");
            Console.WriteLine("7. Quit");

            string userInput = Console.ReadLine();

            switch (userInput)
            {
                case "1":
                    int accountId = _accountService.CreateAccount();
                    Console.WriteLine($"Your Account has been successfully created. Your Account Number is {accountId}.");
                    Thread.Sleep(3000);
                    Run();
                    break;
                case "2":
                    (bool withdrawsuccess, double withdrawBalance) = _transactionService.Withdrawal();
                    if (withdrawsuccess)
                    {
                        Console.WriteLine($"Your Account has been successfully Withdrawn. Your Account balance is {withdrawBalance}.");
                    }
                    else
                    {
                        Console.WriteLine("There was an issue withdrawing from your account");
                    }
                    Thread.Sleep(3000);
                    Run();
                    break;
                case "3":
                    double depositBalance =  _transactionService.Deposit();
                    Console.WriteLine($"Deposit Successful.. Your new account balance is  {depositBalance}");
                    Thread.Sleep(3000);
                    Run();
                    break;
                case "4":
                     bool transferSuceess =  _transactionService.Transfer();
                    if (transferSuceess)
                    {
                        Console.WriteLine($"Transfer Successful");
                    }
                    else
                    {
                        Console.WriteLine($"Transfer UnSuccessful");
                    }
                    Thread.Sleep(3000);
                    Run();
                    break;
                case "5":
                    var balance =_accountService.CheckBalance();
                    if (balance.Equals(typeof(double)))
                    {
                        Console.WriteLine($"Your Account Balance is {balance}");
                    }
                    else
                    {
                        Console.WriteLine(balance);
                    }
                   
                    Thread.Sleep(3000);
                    Run();
                    break;
                case "6":
                    _accountService.ShowAll();
                    Run();
                    break;
                case "7":
                    Environment.Exit(0);
                    break;
                default:
                    Run();
                    break;

            }



        }

    }
}
