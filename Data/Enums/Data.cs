using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Enums
{
    public enum UserRole
    {
        Admin,
        Doctor,
        Staff
    }

    public enum SlotStatus
    {
        Available,
        Reserved,
        Confirmed,
        Cancelled
    }

    public enum AppointmentStatus
    {
        Reserved,
        Confirmed,
        Cancelled,
    }
}
