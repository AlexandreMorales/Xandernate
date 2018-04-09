using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Xandernate.Handler
{
    public interface IEntityHandler<TEntity>
        where TEntity : new()
    {
        /// <summary>
        /// Adds an array of objects in the database.
        /// </summary>
        /// <param name="objs">The array of objects to be added.</param>
        void Add(params TEntity[] objs);


        /// <summary>
        /// Adds or updates an array of objects, verification made by the the id.
        /// </summary>
        /// <param name="objs">The objects to be added or updated.</param>
        void AddOrUpdate(params TEntity[] objs);

        /// <summary>
        /// Adds or updates an array of objects, verification made by the field expressed in the params.
        /// </summary>
        /// <param name="objs">The objects to be added or updated.</param>
        /// <param name="identifierExpression">Lambda showing the field of the object to verify. If not specified the field will be it's id</param>
        /// <returns>The same added object but with its id.</returns>
        void AddOrUpdate(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs);


        /// <summary>
        /// Update the array of objects in the database.
        /// </summary>
        /// <param name="objs">The array of objects to be updated.</param>
        void Update(params TEntity[] objs);

        /// <summary>
        /// Updates an object of the database, updating only the fields expressed in the params or by its id.
        /// </summary>
        /// <param name="objs">The array of objects to be updated.</param>
        /// <param name="identifierExpression">The array of lambdas showing the fields to be updated.</param>
        /// <returns>The same updated object.</returns>
        void Update(Expression<Func<TEntity, object>> identifierExpression, params TEntity[] objs);


        /// <summary>
        /// Finds an object in the database by its primary key.
        /// </summary>
        /// <typeparam name="Att">The primary key field of the object.</typeparam>
        /// <param name="id">The primary key of the object to be found.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        TEntity Find<Att>(Att pk);

        /// <summary>
        /// Finds an object in the database by the field expressed in the params.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="identifierExpression">Lambda showing the field of the object.</param>
        /// <param name="value">The value of the field to find the object.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        TEntity Find<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value);

        /// <summary>
        /// Finds all the objects in the database.
        /// </summary>
        /// <returns>Null if it dont find any, or  all the objects in the database.</returns>
        IEnumerable<TEntity> FindAll();

        /// <summary>
        /// Finds an array of objects in the database if it has the same attribute expressed in the params equals.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="identifierExpression">Lambda showing the field of the object.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        IEnumerable<TEntity> WhereEquals<Att>(Expression<Func<Att>> identifierExpression);

        /// <summary>
        /// Finds an array of objects from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="identifierExpression">Lambda expressing the expression to look in the bank.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> identifierExpression);


        /// <summary>
        /// Removes the object from the database.
        /// </summary>
        /// <param name="obj">The object to be removed.</param>
        void Remove(TEntity obj);

        /// <summary>
        /// Removes the object from the database by its primary key.
        /// </summary>
        /// <typeparam name="Att">The primary key field of the object.</typeparam>
        /// <param name="pk">The primary key of the object to be removed.</param>
        void Remove<Att>(Att pk);

        /// <summary>
        /// Removes the object from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="identifierExpression">Lambda expressing which objects to remove.</param>
        void Remove(Expression<Func<TEntity, bool>> identifierExpression);

        /// <summary>
        /// Removes the object from the database by the field expressed in the params.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="identifierExpression">Lambda showing the field of the object.</param>
        /// <param name="value">The value of the field to remove the object.</param>
        void Remove<Att>(Expression<Func<TEntity, Att>> identifierExpression, Att value);
    }
}
