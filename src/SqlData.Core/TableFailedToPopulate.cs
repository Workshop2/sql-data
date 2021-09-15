using System;

namespace SqlData.Core
{
    public class TableFailedToPopulate : Exception
    {
        public TableFailedToPopulate(string dataFile, Exception innerException)
            : base($"Failed to populate table {dataFile}", innerException)
        { }
    }
}