using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace FieldBook.Models
{
  /// <summary>
  /// Элемент справочника детейлов (таблица cc_details_ref)
  /// </summary>
  [Serializable]
  public class OrderDetailsRefItem
  {
    public static OrderDetailsRefItem CreateFromSqlDataReader(SqlDataReader reader)
    {
      try
      {
        var result = new OrderDetailsRefItem()
        {
          Id = (int)reader["id"],
          DetailName = reader["detail_name"] as string,
          InterfaceType = reader["interface_type"] as string,
          DetailType = reader["detail_type"] as string,
          CreateDate = (DateTime)reader["created"],
          Display = reader["display"] as string,
          Description = reader["description"] as string,
          Author = reader["author"] as string,
          Actual = (decimal)reader["actual"] == 1,
          Archival = (decimal)reader["archival"] == 1
        };

        return result;
      }
      catch (Exception ex)
      {
        throw;
      }

    }

    public OrderDetailsRefItem()
    {
      Values = new OrderDetailsRefItemValues();
    }

    /// <summary>
    /// id PK, identity
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    ///detail_name varchar(100) not null - название детейла (см. cc_order_details)
    /// </summary>
    [DisplayName("Название")]
    public string DetailName { get; set; }
    /// <summary>
    /// interface_type varchar(30) not null - тип интерфейса (см. cc_order_details)
    /// </summary>
    [DisplayName("Тип интерфейса")]
    public string InterfaceType { get; set; }
    /// <summary>
    /// detail_type varchar(30) not null - тип детейла (по-умолчанию, инициализируется строкой 'char') (см. cc_order_details)
    /// </summary>
    [DisplayName("Тип детейла")]
    public string DetailType { get; set; }
    /// <summary>
    /// created datetime not null - дата первого появления этого детейла в заявках в cc_order_details
    /// </summary>
    [DisplayName("Дата создания")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateTime? CreateDate { get; set; }
    /// <summary>
    /// display nvarchar(255) not null - отображаемое пользователям название поля, соответствующего детейлу, в форме редактирования заявки или в сценарии оформления заявки (на будущее). По-умолчанию, писать сюда название детейла из detail_name.
    /// </summary>
    [DisplayName("Отображаемое название")]
    [Required]
    public string Display { get; set; }
    /// <summary>
    /// description nvarchar(1024) - описание детейла или ссылка на подробную документацию на sharepoint-е (по-умолчанию null)
    /// </summary>
    [DisplayName("Описание или ссылка на документацию")]
    [DataType(DataType.MultilineText)]
    [Required]
    public string Description { get; set; }
    /// <summary>
    /// author nvarchar(255) - ФИО пользователя, который добавил описание детейла (по-умолчанию null)
    /// </summary>
    [DisplayName("ФИО автора")]
    public string Author { get; set; }
    /// <summary>
    /// actual numeric(1,0) - 1 запись актуальная, 0 - запись не актуальная (по-умолчанию, 1)
    /// </summary>
    [DisplayName("Актуальный")]
    public bool Actual { get; set; }
    /// <summary>
    /// archival numeric(1,0) - 1 детейл в архиве, 0 - детейл используется, т.е не в архиве (по-умолчанию, 0)
    /// </summary>
    [DisplayName("В архиве")]
    public bool Archival { get; set; }

    /// <summary>
    /// Словарь значений. Берутся из таблицы [cc_details_ref_values]
    /// </summary>
    public OrderDetailsRefItemValues Values { get; set; }
  }


  /// <summary>
  /// Словарь значений детейла.
  /// </summary>
  public class OrderDetailsRefItemValues
  {
    /// <summary>
    /// Словарь значений. Берутся из таблицы [cc_details_ref_values]
    /// </summary>
    public Dictionary<string, string> Data { get; private set; }

    /// <summary>
    /// Есть ли значение у этого детейла?
    /// </summary>
    public bool IsEmpty
    {
      get
      {
        if (Data.Count == 0)
          return true;

        return false;
      }
    }

    public OrderDetailsRefItemValues()
    {
      Data = new Dictionary<string, string>();
    }

    /// <summary>
    /// Добавить или изменить значение детейла.
    /// </summary>
    public void AddOrChangeValue(string value, string displayValue)
    {
      if (Data.ContainsKey(value))
      {
        Data[value] = displayValue;
      }
      else
      {
        Data.Add(value, displayValue);
      }
    }
  }
}