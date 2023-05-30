using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    internal class ControlHandling
    {
        static bool s_Initialized = false;
        static bool? s_SkipCtrl = false;

        public static void DisableSigint()
        {
            if (!s_Initialized)
            {
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = s_SkipCtrl ?? false;
                };
                s_Initialized = true;
            }

            s_SkipCtrl = true;
        }

        public static void EnableSigint()
        {
            s_SkipCtrl = false;
        }
    }
}
