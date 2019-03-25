using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Errors
{
    public class Report : IEnumerable<Error>
    {
        private readonly List<Error> Errors = new List<Error>();

        public bool IsOk => !Errors.Any();

        public void AddError(Error error)
        {
            Errors.Add(error);
        }

        public void AddError(int line, int column, string error)
        {
            AddError(new Error(line, column, error));
        }

        public IEnumerator<Error> GetEnumerator()
        {
            return Errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Error
    {
        public readonly int Line;
        public readonly int Column;
        public readonly string Text;

        public Error(int line, int column, string text)
        {
            Line = line;
            Column = column;
            Text = text;
        }
    }
}
