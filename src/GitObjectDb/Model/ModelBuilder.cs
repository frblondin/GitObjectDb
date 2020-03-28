using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Model
{
    /// <summary>Used to map CLR classes to an EDM model.</summary>
    public abstract class ModelBuilder
    {
        /// <summary>Creates a <see cref="IDataModel"/> based on the configuration performed using this builder.</summary>
        /// <returns>The model that was built.</returns>
        public abstract IDataModel Build();
    }
}
