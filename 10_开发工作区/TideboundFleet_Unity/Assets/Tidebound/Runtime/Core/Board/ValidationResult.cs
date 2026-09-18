using System;
using System.Collections.Generic;
using System.Linq;

namespace Tidebound.Board
{
    public sealed class ValidationIssue
    {
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
        public ValidationIssue(string code, string path, string message)
        { Code = code; Path = path; Message = message; }
        public override string ToString() => $"{Code} at {Path}: {Message}";
    }

    public sealed class ValidationResult
    {
        private readonly List<ValidationIssue> issues = new List<ValidationIssue>();
        public IReadOnlyList<ValidationIssue> Issues => issues.AsReadOnly();
        public bool IsValid => issues.Count == 0;
        internal void Add(string code, string path, string message) => issues.Add(new ValidationIssue(code, path, message));
    }

    public sealed class LevelValidationException : Exception
    {
        public ValidationResult Result { get; }
        public LevelValidationException(ValidationResult result)
            : base(string.Join(Environment.NewLine, result.Issues.Select(x => x.ToString()))) { Result = result; }
    }
}
