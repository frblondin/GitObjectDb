using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Model;

/// <summary>Used to map CLR classes to an EDM model.</summary>
public abstract class ModelBuilder
{
    /// <summary>Gets the function that can update deprecated node to a target type.</summary>
    protected UpdateDeprecatedNode? DeprecatedNodeUpdater { get; private set; }

    /// <summary>Creates a <see cref="IDataModel"/> based on the configuration performed using this builder.</summary>
    /// <returns>The model that was built.</returns>
    public abstract IDataModel Build();

    internal ModelBuilder AddDeprecatedNodeUpdaterInternal(UpdateDeprecatedNode updater)
    {
        DeprecatedNodeUpdater = updater;
        return this;
    }
}

#pragma warning disable SA1402 // File may only contain a single type
/// <summary>Extension methods for adding services to an <see cref="ModelBuilder" />.</summary>
public static class ModelBuilderExtensions
{
    /// <summary>Sets the function that can update deprecated node to a target type.</summary>
    /// <typeparam name="TModelBuilder">The model builder type.</typeparam>
    /// <param name="builder">The source model builder.</param>
    /// <param name="updater">The <see cref="UpdateDeprecatedNode"/>.</param>
    /// <returns>The model builder.</returns>
    public static TModelBuilder AddDeprecatedNodeUpdater<TModelBuilder>(this TModelBuilder builder,
                                                                        UpdateDeprecatedNode updater)
        where TModelBuilder : ModelBuilder
    {
        return (TModelBuilder)builder.AddDeprecatedNodeUpdaterInternal(updater);
    }
}
