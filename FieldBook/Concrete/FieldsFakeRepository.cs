using FieldBook.Abstract;
using FieldBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FieldBook.Concrete
{
  public class FieldsFakeRepository : IFieldsRepository
  {
    private List<OrderDetailsRefItem> fields = new List<OrderDetailsRefItem> {
        new OrderDetailsRefItem{ 
          Actual=true, 
          Archival=false, 
          Author="Тест автор", 
          CreateDate=new DateTime(), 
          Description="Тест description", 
          DetailName="testDetail", 
          DetailType="char", 
          Display="Тест Детейл", 
          Id=1, 
          InterfaceType="operator", 
          Values=null}
      };


    public IEnumerable<OrderDetailsRefItem> Fields
    {
      get { return fields; }
    }
  }
}