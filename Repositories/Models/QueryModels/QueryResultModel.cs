using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models.QueryModels
{
    public class QueryResultModel<TEntity> where TEntity : class
    {
        public int TotalCount { get; set; }
        public TEntity? Data { get; set; }
    }
}
