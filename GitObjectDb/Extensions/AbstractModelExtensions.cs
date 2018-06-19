using System;
using System.Linq.Expressions;

namespace GitObjectDb.Models
{
    public static class AbstractModelExtensions
    {
        public static TModel With<TModel>(this TModel source, Expression<Predicate<TModel>> predicate = null) where TModel : AbstractModel
        {
            return (TModel)source.With(predicate);
        }
    }
}
