using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Host = "Host";
        public const string Guest = "Guest";

        public static readonly List<string> AllRoles = new() { Admin, Host, Guest };
    }
}
