using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Represents a validation error.
    /// </summary>
    [PublicAPI]
    public class DataValidationException : Exception
    {
        /// <summary>
        /// Constructs a validation error for <paramref name="name"/> in which the <paramref name="expected"/> value does not align with the <paramref name="actual"/> value.
        /// </summary>
        /// <param name="name">
        /// The name of the property or operation.
        /// </param>
        /// <param name="expected">
        /// The value that was expected.
        /// </param>
        /// <param name="actual">
        /// The value that was received.
        /// </param>
        public DataValidationException(string name, object expected, object actual) : base($"Property: {name}; Expected '{expected}'; Actual: {actual}.") { }

        /// <summary>
        /// Constructs a validation error for <paramref name="propertyAccess"/> in which the <paramref name="source"/> value does not align with the <paramref name="actual"/> value.
        /// </summary>
        /// <param name="source">
        /// The object from which the named property value was expected to match.
        /// </param>
        /// <param name="propertyAccess">
        /// The name of the property or operation.
        /// </param>
        /// <param name="actual">
        /// The value that was received.
        /// </param>
        public static DataValidationException Create<T>(T source, [NotNull] Expression<Func<T, object>> propertyAccess, object actual)
        {
            return new DataValidationException(Name(propertyAccess), Value(propertyAccess, source), actual);
        }

        /// <summary>
        /// Accesses the property name passed to the <see cref="DataValidationException"/> constructor.
        /// </summary>
        /// <param name="expression">
        /// The member access expression retrieving a property.
        /// </param>
        /// <returns>
        /// The name of the property.
        /// </returns>
        private static string Name<T>([NotNull] Expression<Func<T, object>> expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return ((MemberExpression)expression.Body).Member.Name;
        }


        /// <summary>
        /// Accesses the property name passed to the <see cref="DataValidationException"/> constructor.
        /// </summary>
        /// <param name="expression">
        /// The member access expression retrieving a property.
        /// </param>
        /// <param name="parameter">
        /// The parameter object.
        /// </param>
        /// <returns>
        /// The name of the property.
        /// </returns>
        private static object Value<T>([NotNull] Expression<Func<T, object>> expression, T parameter)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }


            return expression.Compile().Invoke(parameter);
        }
    }
}