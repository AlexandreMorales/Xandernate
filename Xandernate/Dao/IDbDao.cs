using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Xandernate.DAO
{
    public interface IDbDao<TClass>
        where TClass : new()
    {
        /// <summary>
        /// Adds an array of objects in the database.
        /// </summary>
        /// <param name="Objs">The array of objects to be added.</param>
        void Add(params TClass[] Objs);


        /// <summary>
        /// Adds or updates an array of objects, verification made by the the id.
        /// </summary>
        /// <param name="Objs">The objects to be added or updated.</param>
        void AddOrUpdate(params TClass[] Objs);

        /// <summary>
        /// Adds or updates an array of objects, verification made by the field expressed in the params.
        /// </summary>
        /// <param name="Objs">The objects to be added or updated.</param>
        /// <param name="IdentifierExpression">Lambda showing the field of the object to verify. If not specified the field will be it's Id</param>
        /// <returns>The same added object but with its Id.</returns>
        void AddOrUpdate(Expression<Func<TClass, object>> IdentifierExpression, params TClass[] Objs);


        /// <summary>
        /// Update the array of objects in the database.
        /// </summary>
        /// <param name="Objs">The array of objects to be updated.</param>
        void Update(params TClass[] Objs);

        /// <summary>
        /// Updates an object of the database, updating only the fields expressed in the params or by its Id.
        /// </summary>
        /// <param name="Objs">The array of objects to be updated.</param>
        /// <param name="IdentifierExpression">The array of lambdas showing the fields to be updated.</param>
        /// <returns>The same updated object.</returns>
        void Update(Expression<Func<TClass, object>> IdentifierExpression, params TClass[] Objs);


        /// <summary>
        /// Finds an object in the database by its Id.
        /// </summary>
        /// <typeparam name="Att">The Id field of the object.</typeparam>
        /// <param name="Id">The id of the object to be found.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        TClass Find<Att>(Att Id);

        /// <summary>
        /// Finds an object in the database by the field expressed in the params.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="IdentifierExpression">Lambda showing the field of the object.</param>
        /// <param name="Value">Value of the field to find the object.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        TClass Find<Att>(Expression<Func<TClass, Att>> IdentifierExpression, Att Value);

        /// <summary>
        /// Finds all the objects in the database.
        /// </summary>
        /// <returns>Null if it dont find any, or  all the objects in the database.</returns>
        List<TClass> FindAll();

        /// <summary>
        /// Finds an array of objects in the database if it has the same attribute expressed in the params equals.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="IdentifierExpression">Lambda showing the field of the object.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        List<TClass> WhereEquals<Att>(Expression<Func<Att>> IdentifierExpression);

        /// <summary>
        /// Finds an array of objects from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="IdentifierExpression">Lambda expressing the expression to look in the bank.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        List<TClass> Where(Expression<Func<TClass, bool>> IdentifierExpression);


        /// <summary>
        /// Removes the object from the database.
        /// </summary>
        /// <param name="Obj">The object to be removed.</param>
        void Remove(TClass Obj);

        /// <summary>
        /// Removes the object from the database by its Id.
        /// </summary>
        /// <typeparam name="Att">The Id field of the object.</typeparam>
        /// <param name="Id">The Id of the object to be removed.</param>
        void Remove<Att>(Att Id);

        /// <summary>
        /// Removes the object from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="IdentifierExpression">Lambda expressing which objects to remove.</param>
        void Remove(Expression<Func<TClass, bool>> IdentifierExpression);

        /// <summary>
        /// Removes the object from the database by the field expressed in the params.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="IdentifierExpression">Lambda showing the field of the object.</param>
        /// <param name="Value">Value of the field to remove the object.</param>
        void Remove<Att>(Expression<Func<TClass, Att>> IdentifierExpression, Att Value);
    }
}
