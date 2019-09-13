using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask_GZipArchiver.Core.Models
{
    // Utility class for ValidationService
    public class ValidationResult
    {
        public ValidationResult()
        {
            
        }

        public ValidationResult(bool isValid)
        {
            IsValid = isValid;
        }

        public ValidationResult(bool isValid, string message) : this(isValid)
        {
            Message = message;
        }

        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}
