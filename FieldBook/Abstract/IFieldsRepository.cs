using FieldBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldBook.Abstract
{
  interface IFieldsRepository
  {
    IEnumerable<OrderDetailsRefItem> Fields { get; }
  }
}
