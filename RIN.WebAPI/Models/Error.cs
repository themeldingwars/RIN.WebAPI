namespace RIN.WebAPI.Models
{
    public class Error
    {
        public Error() { }

        public Error(string errorCode, string errorMsg = null)
        {
            code    = errorCode;
            message = errorMsg;
        }

        public static class Codes
        {
            // Error Code to test against for if a process was successful or not
            public static string SUCCESS                     = "SUCCESS";

            public static string ERR_HAVE_A_COOKIE           = "ERR_HAVE_A_COOKIE";
            public static string ERR_CHANGE_PASSWORD         = "ERR_CHANGE_PASSWORD";
            public static string ERR_INVALID_TIMESTAMP       = "ERR_INVALID_TIMESTAMP";
            public static string ERR_INVALID_COOKIE          = "ERR_INVALID_COOKIE";
            public static string ERR_EARLY_ACCESS_ONLY       = "ERR_EARLY_ACCESS_ONLY";
            public static string ERR_ACCOUNT_LOCKED          = "ERR_ACCOUNT_LOCKED";
            public static string ERR_STEAM_ID_LINK           = "ERR_STEAM_ID_LINK";
            public static string ERR_AGE_REQUIREMENT_NOT_MET = "ERR_AGE_REQUIREMENT_NOT_MET";
            public static string ERR_EMAIL_MISMATCH          = "ERR_EMAIL_MISMATCH";
            public static string ERR_PASSWORD_MISMATCH       = "ERR_PASSWORD_MISMATCH";
            public static string ERR_UNKNOWN                 = "ERR_UNKNOWN";
            public static string ERR_INCORRECT_USERPASS      = "ERR_INCORRECT_USERPASS";

            public static string ERR_NAME_IN_USE             = "ERR_NAME_IN_USE";
            public static string ERR_NAME_INVALID            = "ERR_NAME_INVALID";
            public static string ERR_NOT_ENOUGH_LETTERS      = "ERR_NOT_ENOUGH_LETTERS";
            public static string ERR_NAME_RESERVED           = "ERR_NAME_RESERVED";
            public static string ERR_NAME_STARTS_WITH_NUMBER = "ERR_NAME_STARTS_WITH_NUMBER";
            public static string ERR_INVALID_CHARACTER       = "ERR_INVALID_CHARACTER";
            public static string ERR_NAME_PROFANITY          = "ERR_NAME_PROFANITY";
            public static string ERR_NAME_TOO_SHORT          = "ERR_NAME_TOO_SHORT";
            public static string ERR_NAME_TOO_LONG           = "ERR_NAME_TOO_LONG";

            public static string ERR_CHAR_NOT_FOUND          = "ERR_CHAR_NOT_FOUND";
            public static string ERR_CHAR_DELETED            = "ERR_CHAR_DELETED";
            public static string ERR_CANNOT_DELETE_COMMANDER = "ERR_CANNOT_DELETE_COMMANDER";

            public static string ERR_ACCOUNT_EXISTS      = "ERR_ACCOUNT_EXISTS";
            public static string ERR_ACCOUNT_CLOSED      = "ERR_ACCOUNT_CLOSED";
            public static string ERR_DUPLICATE_CHARACTER = "ERR_DUPLICATE_CHARACTER";

            // Non client errors, stuff for new web end points
            public static string ERR_NO_EMAIL      = "ERR_NO_EMAIL";
            public static string ERR_NO_TOKEN      = "ERR_NO_TOKEN";
            public static string ERR_TOKEN_MISMACH = "ERR_TOKEN_MISMACH";
        }

        public class ErrorData
        {
            public bool silent { get; set; } = false;
        }

        public string    code     { get; set; }
        public string    message  { get; set; }
        public ErrorData err_data { get; set; } = new ErrorData();
    }
}