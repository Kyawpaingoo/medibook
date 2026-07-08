using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Enums
{
    public struct Status
    {
        public const string Active = "Active";
        public const string Error = "Error";
        public const string Inactive = "Inactive";
    }

    public struct ResponseStatus
    {
        public const string Success = "Success";
        public const string Error = "Error";
        public const string Fail = "Failed";
        public const string DoestNotExist = "DoesNotExist"; 
        public const string Conflict = "Conflict";
    }
}
