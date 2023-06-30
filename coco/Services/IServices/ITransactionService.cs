using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coco.Services.IServices
{
    public interface ITransactionService
    {
        (bool,double) Withdrawal();
        double Deposit();
        bool Transfer();
    }
}
