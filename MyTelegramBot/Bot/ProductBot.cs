﻿using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyTelegramBot.Messages;
using MyTelegramBot.Bot.Core;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using System.Web;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace MyTelegramBot.Bot
{
    public class ProductBot : BotCore
    {
        public const string ModuleName = "Prod";

        private int ProductId { get; set; }

        private ProductViewMessage ProductViewMsg { get; set; }


        /// <summary>
        /// Сообщение со всеми фотографиями товара
        /// </summary>
        private ProductAllPhotoMessage ProductAllPhotoMsg { get; set; }

        private ViewAllProductInCategoryMessage ViewAllProductInCategoryMsg { get; set; }


        public const string GetProductCmd = "GetProduct";

        public const string AddToBasketCmd = "AddToBasket";

        public const string RemoveFromBasketCmd = "RemoveFromBasket";

        public const string MoreInfoProductCmd = "MoreInfoProduct";

        public const string ProductCmd = "/item";

        public const string ViewAllPhotoProductCmd = "ViewAllPhotoProduct";

        public const string CmdViewFeedBack = "ViewFeedBack";

        public const string CmdAddFeedBackProduct = "AddFbToPrdct";

        public const string CmdProductPage = "ProductPage";

        public const string ProductQuestionCmd = "ProductQuestion";

        public const string EnterProductQuestion = "Введите вопрос:";

        /// <summary>
        /// перейти на вторую стр. с кнопками
        /// </summary>
        public const string CmdPage2Buttons = "ProductPage2Btn";

        /// <summary>
        /// вернуться на основую стр. с нопками
        /// </summary>
        public const string CmdBackToMainPageButtons = "BackToMainPageBtn";

        public ProductBot(Update _update) : base(_update)
        {

        }

        protected override void Initializer()
        {
            try
            {
                if (base.Argumetns.Count > 0)
                {
                    ProductId = Argumetns[0];
                    ProductViewMsg = new ProductViewMessage(this.ProductId, BotInfo.Id);
                }

            }

            catch
            {

            }
        }

        public async override Task<IActionResult> Response()
        {

            switch (base.CommandName)
            {
                //Пользователь нажал на кнопку вперед или назад при листнинге товаров
                case GetProductCmd:
                    return await GetProduct();

                //Пользователь нажал на +, добавил товар в корзину в кол-во 1 шт.
                case AddToBasketCmd:
                    return await AddToBasket();

                //Пользователь нажалн на-, удалил товар из корзины в кол-во 1 шт.
                case RemoveFromBasketCmd:
                    return await RemoveFromBasket();

                //Пользователь нажал "Подробнее" 
                case MoreInfoProductCmd:
                    return await SendProductUrl();

                case ViewAllPhotoProductCmd:
                    return await SendAllProductPhoto();

                case CmdProductPage:
                    return await SendProductPage(Argumetns[0], Argumetns[1]);

                case CmdViewFeedBack:
                    return await SendFeedBack();

                case CmdPage2Buttons:
                    return await SendSecondPageButtons(ProductId);

                case CmdBackToMainPageButtons:
                    return await SendMainPageButtons(ProductId);

                case ProductQuestionCmd:
                  return await SendProductQuestion(Argumetns[0]);
            }

            if (base.OriginalMessage.Contains(EnterProductQuestion))
                return await InsertQuestion(OriginalMessage.Substring(EnterProductQuestion.Length,OriginalMessage.Length-EnterProductQuestion.Length), ReplyToMessageText);

            //команда /item
            if (base.CommandName.Contains(ProductCmd))
                return await GetProductCommand();

            else
                return null;
        }


        private async Task<IActionResult> InsertQuestion(string ProductName, string Text)
        {
          var product=  BusinessLayer.ProductFunction.GetProductByName(ProductName);

           var Question = BusinessLayer.ProductFunction.InsertProductQuestion(product.Id, Text, FollowerId);

            if (Question != null)
            {
                await SendMessage(new BotMessage { TextMessage = "Ваш вопрос отправлен операторам. Вернуться к товару /item"+product.Id });

                BotMessage = new Messages.Admin.ProductQuestionAdminViewMessage(Question);

                await SendMessageAllBotEmployeess(BotMessage.BuildMsg());
            }

            return OkResult;
        }

        /// <summary>
        /// Пользователь нажал на кнопку задать вопрос. Появилась форма для ввода сообщения
        /// </summary>
        /// <param name="ProductId"></param>
        /// <returns></returns>
        private async Task<IActionResult> SendProductQuestion(int ProductId)
        {
            if (!BusinessLayer.FollowerFunction.GetFollower(base.FollowerId).Blocked)
            {

                var prod = BusinessLayer.ProductFunction.GetProductById(ProductId);

                await SendForceReplyMessage(EnterProductQuestion + prod.Name);
            }

            else
                await AnswerCallback("Пользователь заблокирован!", true);

            return OkResult;
        }

        private async Task<IActionResult> SendMainPageButtons(int ProductId)
        {
            ProductViewMessage productView = new ProductViewMessage(BusinessLayer.ProductFunction.GetProductById(ProductId));

            await base.EditInlineReplyKeyboard(productView.MainPageButtons());

            return OkResult;
        }


        /// <summary>
        /// отправить вторую стр. кнопок для товара
        /// </summary>
        /// <param name="ProductId"></param>
        /// <returns></returns>
        private async Task<IActionResult> SendSecondPageButtons (int ProductId)
        {
            ProductViewMessage productView = new ProductViewMessage(BusinessLayer.ProductFunction.GetProductById(ProductId));

            await base.EditInlineReplyKeyboard(productView.SecondPageButtons());

            return OkResult;
        }

        /// <summary>
        /// Отправить стр. с товарами
        /// </summary>
        /// <param name="CategoryId">id категории</param>
        /// <param name="PageNumber">номер стр.</param>
        /// <returns></returns>
        private async Task<IActionResult> SendProductPage(int CategoryId, int PageNumber = 1)
        {
            BotMessage = new ViewAllProductInCategoryMessage(CategoryId, PageNumber);

            await EditMessage(BotMessage.BuildMsg());

            return OkResult;
        }

        private async Task<IActionResult> SendFeedBack()
        {
            if (Argumetns.Count == 1)
            {
                BotMessage = new ViewProductFeedBackMessage(Argumetns[0]);
                var mess = BotMessage.BuildMsg();

                if (mess != null)
                    await SendMessage(mess);

            }
            if (Argumetns.Count == 2) // перелистывание отзывов в одном сообщении.
            {
                BotMessage = new ViewProductFeedBackMessage(Argumetns[0], Argumetns[1]);
                var mess = BotMessage.BuildMsg();

                if (mess != null)
                    await EditMessage(mess);
            }


            else
                await AnswerCallback("Отзывы отсутствуют", true);

            return OkResult;
        }

        private async Task<IActionResult> SendAllProductPhoto()
        {
            try
            {
                ProductAllPhotoMsg = new ProductAllPhotoMessage(this.ProductId, BotInfo.Id);

                ProductAllPhotoMsg.BuildMessage();

                //отправляем альбом с фотографиями

                if (ProductAllPhotoMsg.MediaGroupPhoto != null && ProductAllPhotoMsg.MediaGroupPhoto.ListMedia != null && ProductAllPhotoMsg.MediaGroupPhoto.ListMedia.Count > 0)
                {
                    await base.SendMediaPhotoGroup(ProductAllPhotoMsg.MediaGroupPhoto);

                    //следом отправляем кнопку назад
                    await base.SendMessage(ProductAllPhotoMsg);
                }

                else
                {
                    await base.AnswerCallback("Фотографии отсутствуют");
                }
                return OkResult;
            }
            catch
            {
                return OkResult;
            }

        }


        /// <summary>
        /// Добавить позицию в корзину после нажатия кнопки с плюсом (+)
        /// </summary>
        /// <returns></returns>
        private async Task<IActionResult> AddToBasket()
        {
           var basket_total_amount= BusinessLayer.BasketFunction.ProductBasketCount(FollowerId, ProductId, BotInfo.Id);

            var balance= BusinessLayer.StockFunction.CurrentBalance(ProductId);

            if (basket_total_amount + 1 <= balance)
            {
                BusinessLayer.BasketFunction.AddPositionToBasker(FollowerId, ProductId, BotInfo.Id);
                await AnswerCallback("Итого:"+(basket_total_amount+1).ToString(), false);
            }

           else
            {
                await AnswerCallback("В наличии только:" + balance.ToString(), true);
            }

            return OkResult;
        }

        /// <summary>
        /// Срабатывает по команде /product[id] напримиер /product12
        /// </summary>
        /// <returns></returns>
        private async Task<IActionResult> GetProductCommand()
        {
            try
            {
                int id = Convert.ToInt32(base.CommandName.Substring(ProductCmd.Length));

                ProductViewMsg = new ProductViewMessage(id, BotInfo.Id);
                return await GetProduct();
            }

            catch
            {
                return base.OkResult;
            }
        }

        private async Task<IActionResult> GetProduct()
        {
            if (ProductViewMsg != null)
            {
                var mess = ProductViewMsg.BuildMsg();
                if (mess != null && await SendPhoto(mess) != null)
                    return base.OkResult;
                else
                    return base.OkResult;
            }
            else
                return base.OkResult;

        }

        private async Task<IActionResult> RemoveFromBasket()
        {
          int basket_total= BusinessLayer.BasketFunction.RemovePositionFromBasket(FollowerId, ProductId, BotInfo.Id);

           await AnswerCallback("Итого:" + basket_total.ToString(), false);

          return base.OkResult;

        }


        private async Task<IActionResult> SendProductUrl()
        {
            var product = BusinessLayer.ProductFunction.GetProductById(ProductId);
            BotMessage = new ProductViewUrl(product);
            var mess = BotMessage.BuildMsg();
            await SendUrl(mess.TextMessage, mess.MessageReplyMarkup);
            return OkResult;          
        }
    }
}
