using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FieldBook.Models
{
  /// <summary>
  /// Модель поиска детейлов в справочнике. 
  /// Сохраняется в данных сеанса при помощи кастомного биндера (OrderDetailsSearchBinder.cs)
  /// </summary>
  public class OrderDetailsRefSearch
  {
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Show { get; set; }
    public string Contains { get; set; }

    public OrderDetailsRefSearch()
    {
      FromDate = DateTime.Today.AddDays(-1);
      ToDate = DateTime.Today.AddDays(-1);
      Show = "allDetails";
    }
  }
}