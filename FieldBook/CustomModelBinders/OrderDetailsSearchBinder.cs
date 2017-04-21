using FieldBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FieldBook.CustomModelBinders
{
  /// <summary>
  /// Связыватель модели. Сохраняет модель поиска детейлов в данных сеанса.
  /// </summary>
  public class OrderDetailsSearchBinder : IModelBinder
  {
    private const string sessionKey = "OrderDetailsRefSearch";

    public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
    {
      OrderDetailsRefSearch search = null;

      //ЕСЛИ в запросе есть хотя бы одно свойство класса OrderDetailsRefSearch
      if (IsPropertyInRequest(controllerContext))
      {
        //ТО обновляем модель поиска, используя стандартный связыватель модели...
        search = GetFromDefaultBinder(controllerContext, bindingContext);

        //... и сохраняем новую модель поиска в сессии
        SaveInSession(controllerContext, search);

        return search;
      }
      //ИНАЧЕ пытаемся получить объект OrderDetailsRefSearch из сеанса:
      else
      {
        //Получить объект OrderDetailsRefSearch из сеанса
        search = GetFromSession(controllerContext);

        //Создать и сохранить в сессии экземляр OrderDetailsRefSearch, если он не обнаружен в данных сеанса
        if (search == null)
        {
          search = new OrderDetailsRefSearch();
          SaveInSession(controllerContext, new OrderDetailsRefSearch());
        }

        return search;
      }
    }

    #region Вспомогательные методы

    private OrderDetailsRefSearch GetFromDefaultBinder(ControllerContext controllerContext, ModelBindingContext bindingContext)
    {
      var search = new DefaultModelBinder().BindModel(controllerContext, bindingContext);
      return (OrderDetailsRefSearch)search;
    }

    private void SaveInSession(ControllerContext controllerContext, object objToSave)
    {
      if (controllerContext.HttpContext.Session != null)
      {
        controllerContext.HttpContext.Session[sessionKey] = objToSave;
      }
    }

    private OrderDetailsRefSearch GetFromSession(ControllerContext controllerContext)
    {
      if (controllerContext.HttpContext.Session != null)
      {
        return (OrderDetailsRefSearch)controllerContext.HttpContext.Session[sessionKey];
      }
      return null;
    }

    /// <summary>
    /// ЕСЛИ в данных, которые передаются методом POST есть хотя бы одно свойство класса OrderDetailsRefSearch
    /// TO вернет 'true',
    /// ИНАЧЕ - 'false'
    /// </summary>
    /// <param name="controllerContext"></param>
    /// <returns></returns>
    private bool IsPropertyInRequest(ControllerContext controllerContext)
    {
      OrderDetailsRefSearch search = new OrderDetailsRefSearch();
      foreach (var key in controllerContext.HttpContext.Request.Form.AllKeys)
      {
        if (search.GetType().GetProperties().Where(prop => prop.Name == key) != null)
        {
          return true;
        }
      }
      return false;
    }

    #endregion
  }
}