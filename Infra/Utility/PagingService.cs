using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infra.Utility
{
    public class PagingService<T>
    {
        public static async Task<Model<T>> getPaging(int page, int pageSize, IQueryable<T> result, string additionaldata = "")
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;
            try
            {
                var totalCount = result.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var dataList = await result.Skip(pageSize * (page - 1)).Take(pageSize).ToListAsync();
                Model<T> model = new Model<T>();
                model.Results = dataList;
                model.TotalCount = totalCount;
                model.TotalPages = totalPages;
                model.AdditionalData = additionaldata;
                return model;
            }
            catch (Exception ex)
            {

            }

            return null;

        }

        public static async Task<Model<T>> getPagingList(int page, int pageSize, List<T> result, string additionaldata = "")
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;
            try
            {
                var totalCount = result.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var dataList = result.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
                Model<T> model = new Model<T>();
                model.Results = dataList;
                model.TotalCount = totalCount;
                model.TotalPages = totalPages;
                model.AdditionalData = additionaldata;
                return model;
            }
            catch (Exception ex)
            {

            }

            return null;

        }
    }

    public class Model<T>
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string prevLink { get; set; }
        public string nextLink { get; set; }
        public IEnumerable<T> Results { get; set; }
        public string AdditionalData { get; set; }
    }

    public static class SORTLIT<T>
    {

        public static IQueryable<T> Sort(IQueryable<T> source, string Field, string Direction = "asc")
        {
            var type = typeof(T);
            var property = type.GetProperty(Field);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyAccess, typeof(object)), parameter);
            if (Direction == "asc")
            {
                return Queryable.OrderBy(source, orderByExp);
            }
            else
            {
                return Queryable.OrderByDescending(source, orderByExp);
            }
        }
    }
}
