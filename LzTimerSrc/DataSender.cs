using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkot.LzTimer
{
    class DataSender : TimeTableUpdateListener
    {
        private readonly TokenReceiver tokenReceiver;

        public DataSender(TokenReceiver tokenReceiver)
        {
            this.tokenReceiver = tokenReceiver;
        }

        public void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                return;
            }


        }
    }
}
