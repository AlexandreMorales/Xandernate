using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Xandernate.Dao
{
    public interface IDbDao<Classe>
    {
        /// <summary>
        /// Adds an object in the database.
        /// </summary>
        /// <param name="obj">The object to be added.</param>
        /// <returns>The same added object but with its Id.</returns>
        Classe Add(Classe obj);

        /// <summary>
        /// Finds an object in the database by its Id.
        /// </summary>
        /// <param name="id">The id of the object to be found.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        Classe Find(Object id);

        /// <summary>
        /// Finds an object in the database by the field expressed in the params.
        /// </summary>
        /// <param name="expression">Lambda showing the field of the object.</param>
        /// <param name="value">Value of the field to find the object.</param>
        /// <returns>Null if it dont find any, or the object found.</returns>
        Classe Find<Att>(Expression<Func<Classe, Att>> expression, Att value);

        /// <summary>
        /// Finds all the objects in the database.
        /// </summary>
        /// <returns>Null if it dont find any, or  all the objects in the database.</returns>
        Classe[] FindAll();

        /// <summary>
        /// Removes the object from the database.
        /// </summary>
        /// <param name="obj">The object to be removed.</param>
        void Remove(Classe obj);

        /// <summary>
        /// Removes the object from the database by its Id.
        /// </summary>
        /// <param name="id">The Id of the object to be removed.</param>
        void Remove(Object id);

        /// <summary>
        /// Removes the object from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="expression">Lambda expressing which objects to remove.</param>
        void Remove(Expression<Func<Classe, bool>> expression);

        /// <summary>
        /// Removes the object from the database by the field expressed in the params.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="expression">Lambda showing the field of the object.</param>
        /// <param name="value">Value of the field to remove the object.</param>
        void Remove<Att>(Expression<Func<Classe, Att>> expression, Att value);

        /// <summary>
        /// Adds an array of objects in the database.
        /// </summary>
        /// <param name="objs">The array of objects to be added.</param>
        void AddRange(params Classe[] objs);

        /// <summary>
        /// Adds or updates an array of objects, verification made by its Id.
        /// </summary>
        /// <param name="objs">The array of objects to be added or updated.</param>
        void AddOrUpdate(params Classe[] objs);

        /// <summary>
        /// Adds or updates an array of objects, verification made by the field expressed in the params.
        /// </summary>
        /// <param name="expression">Lambda showing the field of the object to verify.</param>
        /// <param name="objs">The array of objects to be added or updated.</param>
        void AddOrUpdate<Att>(Expression<Func<Classe, Att>> expression, params Classe[] objs);

        /// <summary>
        /// Updates an object of the database.
        /// </summary>
        /// <param name="obj">The object to be updated.</param>
        /// <returns>The same updated object.</returns>
        Classe Update(Classe obj);

        /// <summary>
        /// Updates an object of the database, updating only the fields expressed in the params.
        /// </summary>
        /// <param name="obj">The object to be updated.</param>
        /// <param name="expression">The array of lambdas showing the fields to be updated.</param>
        /// <returns>The same updated object.</returns>
        Classe Update(Classe obj, Expression<Func<Classe, object>> expressions);

        /// <summary>
        /// Finds an array of objects in the database if it has the same attribute expressed in the params equals.
        /// </summary>
        /// <typeparam name="Att">The field of the object.</typeparam>
        /// <param name="expression">Lambda showing the field of the object.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        Classe[] WhereEquals<Att>(Expression<Func<Att>> expression);

        /// <summary>
        /// Finds an array of objects from the database by the expression expressed in the params.
        /// </summary>
        /// <param name="expression">Lambda expressing the expression to look in the bank.</param>
        /// <returns>Null if it dont find any, or an array objects found.</returns>
        Classe[] Where(Expression<Func<Classe, bool>> expression);
    }
}
