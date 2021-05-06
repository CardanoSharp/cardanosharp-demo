﻿using CardanoSharp.DbSync.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CardanoSharp.DbSync.EntityFramework.Models;
using Moq;
using Npgsql;
using System;
using System.IO;
using WebUI;
using Xunit;
using System.Threading.Tasks;
using ApplicationIntegrationTests.Builders;
using System.Linq;

namespace ApplicationIntegrationTests
{
    public class DatabaseFixture : IDisposable
    {
        private readonly CardanoContext _cardanoContext;


        public DatabaseFixture()
        {
            var builder = new ConfigurationBuilder()
                        .Build();

            var options = new DbContextOptionsBuilder<CardanoContext>()
                      .UseInMemoryDatabase(databaseName: "Cardano")
                      .Options;

            _cardanoContext = new CardanoContext(options, builder);
        }

        [Fact]
        public async Task FirstTimeSetUp()
        {
            //arrange
            BlockBuilder.GenerateBlocks(30, _cardanoContext);


            //act
            var firstBlock = await _cardanoContext.Blocks.FirstOrDefaultAsync();

            //assert
            Assert.True(firstBlock.EpochNo is 1);
            
        }

        [Fact]
        public async void GetCurrentEpochTest()
        {
            BlockBuilder.GenerateBlocks(30, _cardanoContext);


            var currentEpoch = await _cardanoContext.Blocks
                        .MaxAsync(s => s.EpochNo);

            Assert.Equal(30, currentEpoch.Value);
            
 
        }
        
        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        public async void GetTransactionsInEpochTest(int expected)
        {
            BlockBuilder.GenerateBlocks(30, _cardanoContext);

            var txCount = await _cardanoContext.Blocks
                .Where(x => x.EpochNo == expected - 1).FirstOrDefaultAsync(); 
            
            Assert.Equal(expected * 5, txCount.TxCount + 5);

        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async void GetTransactionDetailsTest(int iD)
        {
            TransactionBuilder.GenerateTransactions(10, _cardanoContext);

            var transactionDetails = await _cardanoContext.Txes.Where(s => s.Hash == BitConverter.GetBytes(iD))
                                            .Include(s => s.Block)
                                            .Include(s => s.TxOuts)
                                            .Include(s => s.TxMetadata)
                                            .Include(s => s.TxInTxInNavigations)
                                            .ThenInclude(s => s.TxOut)
                                            .ThenInclude(s => s.TxOuts)
                                            .FirstOrDefaultAsync();

            Assert.Equal(BitConverter.GetBytes(iD), transactionDetails.Hash);
            Assert.Equal(iD + 1, transactionDetails.Block.SlotNo);
            Assert.Equal(iD + 2, transactionDetails.Block.EpochNo);
            Assert.Equal(iD + 3, transactionDetails.Fee);
            Assert.Equal(iD + 4, transactionDetails.OutSum);
            Assert.Equal("TxOutAddress" + iD.ToString(), transactionDetails.TxOuts.Select(s => s.Address).FirstOrDefault());
            Assert.Equal("Hello There Friends From index : " + iD.ToString(), transactionDetails.TxMetadata.Select(s => s.Json).FirstOrDefault());
            //Assert.Equal("TxOutAddress" + iD.ToString(), transactionDetails.TxInTxInNavigations.Select(s => s.TxOut.TxOuts.Select(s => s.Address)).FirstOrDefault().ToString()); 
        }

        public void Dispose()
        {
            _cardanoContext.Dispose(); 
        }
    }
}
