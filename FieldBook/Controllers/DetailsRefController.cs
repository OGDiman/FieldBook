using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Principal;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using FieldBook.Models;
using FieldBook.CustomModelBinders;

namespace CCRWIN.Controllers
{
  [HandleError(Order = 1, ExceptionType = typeof(Exception), View = "_Error500")]
  public class DetailsRefController : Controller
  {

    private readonly IDBProvider _dbProvider;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="dbProvider">Объект для взаимодействия с БД.</param>
    public DetailsRefController(IDBProvider dbProvider)
    {
      _dbProvider = dbProvider;
    } // DetailsRefController

    #region Методы для отображения списка детейлов

    /// <summary>
    /// Для первоначального отображения справочника детейлов.
    /// </summary>
    /// <param name="searchObject">Модель поиска</param>
    /// <returns></returns>
    public ActionResult Index([ModelBinder(typeof(OrderDetailsSearchBinder))] OrderDetailsRefSearch searchObject)
    {
      return View("Index", searchObject);
    }

    /// <summary>
    /// Отображает список детейлов
    /// </summary>
    /// <param name="searchObject">Модель поиска</param>
    /// <returns></returns>
    public PartialViewResult GetDetails([ModelBinder(typeof(OrderDetailsSearchBinder))] OrderDetailsRefSearch searchObject)
    {
      var fromDate = searchObject.FromDate;
      var toDate = searchObject.ToDate;
      var show = searchObject.Show;
      var subString = searchObject.Contains;

      List<OrderDetailsRefItem> data = GetAllDetails();

      //Фильтруем по датам
      if (fromDate.HasValue && toDate.HasValue)
        data = data.Select(d => d).Where(d => d.CreateDate >= fromDate && d.CreateDate < toDate.Value.AddDays(1)).ToList();

      if (show != "" && show != "allDetails")
      {
        if (show == "onlyNewDetails")
        {
          //Показываем детейлы без описания
          data = data.Where(d => d.Description == null).ToList();
        }
        if (show == "onlyDocumentedDetails")
        {
          //Показываем детейлы c описанием
          data = data.Where(d => d.Description != null).ToList();
        }
      }

      if (subString != null && subString != "")
      {
        data = data.Where(d => d.DetailName.Contains(subString) || d.Display.Contains(subString) || (d.Description != null && d.Description.Contains(subString))).ToList();
      }

      //Сортируем по названию
      data = data.OrderBy(d => d.DetailName).ToList();

      return PartialView(data);
    }

    #endregion

    #region Методы для отображения информации по конкретному детейлу

    /// <summary>
    /// Отображает информацию по детейлу
    /// </summary>
    /// <param name="id">id актуальной записи о детейле в БД</param>
    /// <returns></returns>
    public ViewResult Show(int id)
    {
      OrderDetailsRefItem detail = GetDetailById(id);
      return View(detail);
    }

    /// <summary>
    /// Возвращает список шаблонов оформления, которых используется детейл с заданным именем
    /// </summary>
    /// <param name="detailName">Имя детейла</param>
    public PartialViewResult GetFilesWhichContains(string detailName)
    {
      string pathToTemplates = HttpContext.Server.MapPath("/xml");
      string searchString = String.Format("f=\"{0}\"", detailName);
      string[] orderTemplates = Directory.GetFiles(pathToTemplates, "problem*");
      List<string> fileList = new List<string>();

      foreach (var file in orderTemplates)
      {
        foreach (string line in System.IO.File.ReadLines(file))
        {
          if (line.Contains(searchString))
          {
            fileList.Add(Path.GetFileName(file));
            break;
          }
        }
      }

      return PartialView("FileList", fileList);
    }

    #endregion

    #region Методы для изменения детейла

    /// <summary>
    /// Отображает страницу изменения детейла.
    /// </summary>
    /// <param name="id">id записи о детейле в таблице cc_details_ref</param>
    /// <returns></returns>
    [HttpGet]
    public ViewResult Edit(int id)
    {
      OrderDetailsRefItem data = GetDetailById(id);
      return View("Edit", data);
    }

    /// <summary>
    /// Сохраняет изменения детейла.
    /// </summary>
    /// <param name="item">объект записи о детейле в таблице cc_details_ref с обновленными значениями</param>
    /// <returns></returns>
#if !DEBUG
        [CustomAuthorize(Roles = @"CRM_Редакторы описаний детейлов")]
#endif
    [HttpPost]
    public ActionResult Edit(OrderDetailsRefItem item)
    {
      if (ModelState.IsValid)
      {
        //
        // См. #14822 Логика справочника следующая. Фактически он является историей изменения информации о детейлах.
        // Поэтому изменение выливается в добавление нового варианта. Новый вариант становится активным, старый перестает быть активным.
        // В требовании записано: сочетание полей detail_name+interface_type+display+description = unique constraint
        // Т.к. SQL-сервер отказывается создавать уникальный индекс по "длинному" ключу, то контролируем уникальность программно здесь

        if (IsUniqueRecord(item))
        {
          SaveNewRecord(item);
          return RedirectToAction("Index");
        }
        else
        {
          ViewBag.ErrorMessage = "Заданное сочетание названия детейла, типа интерфейса, отображаемого имени и описания не является уникальным";
          return View("Edit", item);
        }
      }
      else
      {
        return View("Edit", item);
      }
    }

    /// <summary>
    /// Убирает детейл в архив, или выводит его из архива
    /// </summary>
    public RedirectToRouteResult ChangeArchivalState(OrderDetailsRefItem item)
    {
      item.Archival = !item.Archival;
      long idOfNewRecord = SaveNewRecord(item);
      return RedirectToAction("Edit", new { id = idOfNewRecord });
    }

    /// <summary>
    /// Сохраняет, обновляет или удаляет значение детейла. Удаляет значение, если displayValue == "".
    /// </summary>
#if !DEBUG
        [CustomAuthorize(Roles = @"CRM_Редакторы описаний детейлов")]
#endif
    public RedirectToRouteResult SetValue(string detailId, string detailName, string interfaceType, string value, string displayValue)
    {
      if (displayValue != "")
      {
        InsertOrUpdateValue(detailName, interfaceType, value, displayValue);
      }
      else
      {
        DeleteValue(detailName, interfaceType, value);
      }

      return RedirectToAction("Edit", new { id = detailId });
    }

    #endregion

    //----------------------------------------------

    #region Вспомогательные методы

    /// <summary>
    /// Возвращает из БД список всех актуальных детейлов
    /// </summary>
    private List<OrderDetailsRefItem> GetAllDetails()
    {
      var data = new List<OrderDetailsRefItem>();
      var sql = "select * from cc_details_ref where actual = 1";
      using (var command = _dbProvider.DB.CreateSqlCommand(sql))
      {
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var detailsRefItem = OrderDetailsRefItem.CreateFromSqlDataReader(reader);
            data.Add(detailsRefItem);
          }
        }
      }

      return data;
    }

    /// <summary>
    /// Возвращает объект детейла по id записи в БД
    /// </summary>
    /// <param name="id">id записи в БД</param>
    private OrderDetailsRefItem GetDetailById(int id)
    {
      OrderDetailsRefItem data = null;

      var sql = "select * from cc_details_ref where id = " + id;
      using (var command = _dbProvider.DB.CreateSqlCommand(sql))
      {
        using (var reader = command.ExecuteReader())
        {
          if (reader.Read())
          {
            data = OrderDetailsRefItem.CreateFromSqlDataReader(reader);
            try
            {
              // Автор для новой записи должен заполняться автоматически
              WindowsIdentity windowsIdentity = System.Web.HttpContext.Current.Request.LogonUserIdentity;
              data.Author = windowsIdentity.Name;
              if (data.Author == null)
              {
                data.Author = "Неизвестный пользователь";
              }
            }
            catch
            {
              data.Author = "Неизвестный пользователь";
            }
          }
        }
      }

      if (data.DetailName != null)
      {
        using (var command = _dbProvider.DB.CreateSqlCommand(""))
        {
          command.CommandText = "SELECT value, display_value FROM [dbo].[cc_details_ref_values] WHERE [detail_name] = @detail_name AND [interface_type] = @interface_type";
          command.Parameters.AddWithValue("@detail_name", data.DetailName);
          command.Parameters.AddWithValue("@interface_type", data.InterfaceType);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              data.Values.AddOrChangeValue((string)reader["value"], (string)reader["display_value"]);
            }
          }
        }
      }

      return data;
    }

    /// <summary>
    /// Добавить или обновить значение детейла
    /// </summary>
    /// <param name="detailName">Название детейла</param>
    /// <param name="value">Значение детейла</param>
    /// <param name="displayValue">Отображаемое значение детейла</param>
    private void InsertOrUpdateValue(string detailName, string interfaceType, string value, string displayValue)
    {
      _dbProvider.DB.BeginTransaction(IsolationLevel.ReadCommitted);
      try
      {
        using (var command = _dbProvider.DB.CreateSqlCommand(""))
        {
          command.CommandText = @"UPDATE 
                                        [dbo].[cc_details_ref_values]
                                      SET 
                                        [detail_name] = @detail_name, [interface_type] = @interface_type, [value] = @value, [display_value] = @display_value
                                      WHERE
                                        [detail_name] = @detail_name AND [interface_type] = @interface_type AND [value] = @value

                                      IF @@ROWCOUNT = 0
                                      BEGIN
                                        INSERT INTO 
                                          [dbo].[cc_details_ref_values] ([detail_name], [interface_type], [value], [display_value])
                                        VALUES
                                          (@detail_name, @interface_type, @value, @display_value)
                                      END";
          command.Parameters.AddWithValue("@detail_name", detailName);
          command.Parameters.AddWithValue("@interface_type", interfaceType);
          command.Parameters.AddWithValue("@value", value);
          command.Parameters.AddWithValue("@display_value", displayValue);

          command.ExecuteNonQuery();

          _dbProvider.DB.CommitTransaction();
        }
      }
      catch (Exception ex)
      {
        _dbProvider.DB.RollbackTransaction();
        throw;
      }
    }

    /// <summary>
    /// Удалить значение детейла
    /// </summary>
    /// <param name="detailName">Название детейла</param>
    /// <param name="value">Значение детейла</param>
    private void DeleteValue(string detailName, string interfaceType, string value)
    {
      _dbProvider.DB.BeginTransaction(IsolationLevel.ReadCommitted);
      try
      {
        using (var command = _dbProvider.DB.CreateSqlCommand(""))
        {
          command.CommandText = @"DELETE FROM 
                                      [dbo].[cc_details_ref_values]
                                    WHERE 
                                      [detail_name] = @detail_name AND [interface_type] = @interface_type AND [value] = @value";

          command.Parameters.AddWithValue("@detail_name", detailName);
          command.Parameters.AddWithValue("@interface_type", interfaceType);
          command.Parameters.AddWithValue("@value", value);

          command.ExecuteNonQuery();

          _dbProvider.DB.CommitTransaction();
        }
      }
      catch (Exception ex)
      {
        _dbProvider.DB.RollbackTransaction();
        throw;
      }
    }

    /// <summary>
    /// Возвращает false, если сочетание сочетание полей item.DetailName + item.InterfaceType + item.Display + item.Description не является уникальным
    /// </summary>
    /// <param name="item">Проверяемый объект записи о детейле</param>
    private bool IsUniqueRecord(OrderDetailsRefItem item)
    {
      int count;
      using (var command = _dbProvider.DB.CreateSqlCommand(""))
      {
        command.CommandText = @"SELECT COUNT(*) FROM [dbo].[cc_details_ref] r " +
            @" WHERE r.detail_name = @detail_name AND r.interface_type = @interface_type AND r.display = @display AND (r.description = @description OR r.description IS NULL AND @description IS NULL)";
        command.Parameters.AddWithValue("@detail_name", item.DetailName);
        command.Parameters.AddWithValue("@interface_type", item.InterfaceType);
        command.Parameters.AddWithValue("@display", item.Display);
        command.Parameters.AddWithValue("@description", (object)item.Description ?? (object)DBNull.Value);

        count = (int)command.ExecuteScalar();
      }

      if (count > 0)
        return false;
      return true;
    }

    /// <summary>
    /// Добавляет новую запись, а старую запись помечает как не актуальную
    /// </summary>
    /// <param name="item">Объект записи о детейле с обновленными данными</param>
    /// <returns>id новой записи о детейле</returns>
    private long SaveNewRecord(OrderDetailsRefItem item)
    {
      _dbProvider.DB.BeginTransaction(IsolationLevel.ReadCommitted);
      try
      {
        using (var command = _dbProvider.DB.CreateSqlCommand(""))
        {
          command.CommandText = @"INSERT INTO [dbo].[cc_details_ref] ([detail_name],[interface_type],[detail_type],[created],[display],[description],[author],[actual],[archival])" +
          @" VALUES (@detail_name,@interface_type,@detail_type,@created,@display,@description,@author,@actual,@archival); " +
              @" UPDATE [dbo].[cc_details_ref] SET actual = 0 WHERE id = @id";
          command.Parameters.AddWithValue("@detail_name", item.DetailName);
          command.Parameters.AddWithValue("@interface_type", item.InterfaceType);
          command.Parameters.AddWithValue("@detail_type", item.DetailType);
          command.Parameters.AddWithValue("@created", item.CreateDate);
          command.Parameters.AddWithValue("@display", item.Display);
          command.Parameters.AddWithValue("@description", (object)item.Description ?? (object)DBNull.Value);
          command.Parameters.AddWithValue("@author", item.Author);
          command.Parameters.AddWithValue("@actual", true);// Новая запись - активная
          command.Parameters.AddWithValue("@archival", item.Archival);
          command.Parameters.AddWithValue("@id", item.Id);// Для второй части скрипта

          command.ExecuteNonQuery();

          command.CommandText = "SELECT @@IDENTITY";
          long idOfNewRecord = Convert.ToInt64(command.ExecuteScalar());

          _dbProvider.DB.CommitTransaction();
          return idOfNewRecord;
        }
      }
      catch (Exception ex)
      {
        _dbProvider.DB.RollbackTransaction();
        throw;
      }
    }

    #endregion

    /// <summary>
    /// Временный метод. Получить значения для детейла из файловой системы.
    /// </summary>
    /// <param name="detailName"></param>
    /// <returns></returns>
    private List<Tuple<string, string>> GetValuesForDetail(string detailName)
    {
      string pathToTemplates = HttpContext.Server.MapPath("/xml");
      string[] orderTemplates = Directory.GetFiles(pathToTemplates, "problem*");

      List<Tuple<string, string>> list = new List<Tuple<string, string>>();

      foreach (var file in orderTemplates)
      {
        using (XmlReader reader = XmlReader.Create(file))
        {
          try
          {
            XElement xmlFile = XElement.Load(reader);
            var lis1 = xmlFile
              .Descendants("item")
              .Where(x => x.Attribute("f") != null && x.Attribute("f").Value == detailName);
            list.AddRange(lis1.Descendants().Where(x => x.Attribute("value") != null).Select(x => new Tuple<string, string>(x.Attribute("value").Value, x.Value)).ToList());
          }
          catch { }
        }
      }

      return list.GroupBy(x => x.Item1.ToLower()).Select(x => x.First()).ToList();
    }
  }
}