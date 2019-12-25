using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = new StringBuilder();
            List<Tuple<string, EFComparison, object>> conditions = new List<Tuple<string, EFComparison, object>>();
            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                object value;
                switch (random.Next(0, 2))
                {
                    case 0:
                        value = "1";
                        conditions.Add(new Tuple<string, EFComparison, object>(nameof(User.Name), EFComparison.Contains, value));//name 包含 1
                        text.Append($"&& Name Contains {value} ");
                        break;
                    case 1:
                        value = 50;
                        conditions.Add(new Tuple<string, EFComparison, object>(nameof(User.Age), EFComparison.GreaterThan, value));//年龄大于50
                        text.Append($"&& Age > {value} ");
                        break;
                    case 2:
                        value = true;
                        conditions.Add(new Tuple<string, EFComparison, object>(nameof(User.Sex), EFComparison.Equal, value));//sex = true;
                        text.Append($"&& Sex  = {value} ");
                        break;
                    default:
                        break;
                }
            } 



            Console.WriteLine($"条件:{text}");


            var exp = GetExpression<User>(conditions);

            var func = exp.Compile();

            using (var db = new DB())
            {
                Console.WriteLine("开始查询");
                foreach (var item in func(db))
                {
                    Console.WriteLine($"Name:{item.Name} Age:{item.Age} Sex:{(item.Sex ? "男" : "女")}");
                }
                Console.WriteLine("结束查询");
            }

            Console.ReadKey();
        }


        static Expression<Func<DbContext, IEnumerable<TModel>>> GetExpression<TModel>(IEnumerable<Tuple<string, EFComparison, object>> conditions)
        {
            //完整表达式 return db.Set<T>().Where();   
            //db.Set<T>()
            var db = Expression.Parameter(typeof(DbContext));//参数db
            var body = Expression.Call(db, nameof(DbContext.Set), new Type[] { typeof(TModel) });//db.Set<T>()

            //query.Where()
            var queryExp = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(typeof(TModel)));
            var modelExp = Expression.Parameter(typeof(TModel));
            var whereConditionExp = GetExpression(modelExp, conditions);
            var whereConditionLambdaExp = Expression.Lambda(whereConditionExp,modelExp);//Expression<Func<TModel, bool>> predicate
            body = Expression.Call(typeof(Queryable), nameof(Queryable.Where), new Type[] { typeof(TModel) }, body, whereConditionLambdaExp);////db.Set<T>().Where(Expression<Func<TModel, bool>> predicate)


            return Expression.Lambda<Func<DbContext, IEnumerable<TModel>>>(body, db);
        }

        /// <summary>
        /// //i.Key>0 && i.Key<1||i.
        /// </summary>
        /// <param name="modelExp"></param>
        /// <returns></returns>
        static Expression GetExpression(ParameterExpression modelExp, IEnumerable<Tuple<string, EFComparison, object>> conditions)
        {
            LinkedList<Tuple<string, EFComparison, object>> conditionsLink = new LinkedList<Tuple<string, EFComparison, object>>(conditions);
            var first = conditionsLink.First;
            var item = first.Value;
            var body = GetExpression(modelExp,item.Item1,item.Item2,item.Item3);
            var node = first.Next;
            while (node != null)
            {
                item = node.Value;
                var exp = GetExpression(modelExp, item.Item1, item.Item2, item.Item3);
                if (true)//and
                {
                    body = Expression.AndAlso(body, exp);//i.Key>0 && i.Key<1
                }
                else//or
                {
                    body = Expression.OrElse(body, exp);//i.Key>0 || i.Key<1
                }

                node = node.Next;
            }
            return body;
        }

        /// <summary>
        /// i.Property==Value
        /// </summary>
        /// <param name="modelExp"></param>
        /// <param name="propertyName"></param>
        /// <param name="comparison"></param>
        /// <param name="comparisonValue"></param>
        /// <returns></returns>
        static Expression GetExpression(ParameterExpression modelExp, string propertyName,EFComparison comparison,object comparisonValue)
        {
            var propertyExp = Expression.Property(modelExp, propertyName);//i.Property
            var valueExp = Expression.Constant(comparisonValue);//value
            var trueExp = Expression.Constant(true);//true
            switch (comparison)
            {
                case EFComparison.Equal:
                    return Expression.Equal(propertyExp, valueExp);//i.PropertyName = value;
                case EFComparison.NotEqual:
                    return Expression.NotEqual(propertyExp, valueExp);//i.PropertyName != true
                case EFComparison.GreaterThan:
                    return Expression.GreaterThan(propertyExp, valueExp);//i.PropertyName > value;
                case EFComparison.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(propertyExp, valueExp);//i.PropertyName >= value;
                case EFComparison.LessThan:
                    return Expression.LessThan(propertyExp, valueExp);//i.PropertyName < value;
                case EFComparison.LessThanOrEqual:
                    return Expression.LessThanOrEqual(propertyExp, valueExp);//i.PropertyName <= value;
                case EFComparison.Contains:
                    return Expression.Call(propertyExp, nameof(string.Contains), null, valueExp);// i.PropertyName.Contains(value);
                case EFComparison.NotContains:
                    var containsExp = Expression.Call(propertyExp, nameof(string.Contains), null, valueExp);
                    return Expression.NotEqual(containsExp, trueExp);//i.PropertyName.Contains(value) != true
                case EFComparison.StartWith:
                    return Expression.Call(propertyExp, nameof(string.StartsWith), null, valueExp);// i.PropertyName.StartsWith(value);
                case EFComparison.EndWith:
                    return Expression.Call(propertyExp, nameof(string.EndsWith), null, valueExp);// i.PropertyName.EndsWith(value);
                default:
                    break;
            }
            return null;
        }

    }


    public enum EFComparison
    {
        /// <summary>
        /// 等于 
        /// ==
        /// </summary>
        Equal,
        /// <summary>
        /// 不等于 
        /// !=
        /// </summary>
        NotEqual,
        /// <summary>
        /// 大于
        /// >
        /// </summary>
        GreaterThan,
        /// <summary>
        /// 大于或等于
        /// >=
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// 小于
        /// </summary>
        LessThan,
        /// <summary>
        /// 小于或等于
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// 包含
        /// </summary>
        Contains,
        /// <summary>
        /// 不包含
        /// </summary>
        NotContains,
        /// <summary>
        /// 开头
        /// </summary>
        StartWith,
        /// <summary>
        /// 尾部
        /// </summary>
        EndWith,
    }

}
