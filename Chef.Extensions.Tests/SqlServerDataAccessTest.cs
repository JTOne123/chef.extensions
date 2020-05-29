﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using Chef.Extensions.DbAccess;
using Chef.Extensions.DbAccess.Fluent;
using Chef.Extensions.DbAccess.SqlServer;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chef.Extensions.Tests
{
    [TestClass]
    public class SqlServerDataAccessTest
    {
        private static readonly IDataAccessFactory DataAccessFactory = SqlServerDataAccessFactory.Instance;

        internal Club Club => new Club { Id = 25 };

        [TestInitialize]
        public void Startup()
        {
            SqlServerDataAccessFactory.Instance.AddConnectionString("Advertisement", @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=Advertisement;Integrated Security=True");
            SqlServerDataAccessFactory.Instance.AddConnectionString("Advertisement2", @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=Advertisement;Integrated Security=True");
            SqlServerDataAccessFactory.Instance.AddConnectionString("Advertisement3", @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=Advertisement;Integrated Security=True");

            SqlServerDataAccessFactory.Instance.AddUserDefinedTable<Club>(
                "ClubType",
                new Dictionary<string, System.Type> { ["ClubID"] = typeof(int), ["Name"] = typeof(string), ["IsActive"] = typeof(bool) });
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_InnerJoin_Two_Tables()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            Expression<Func<User, Department, bool>> join = (x, y) => x.DepartmentId == y.DepId;
            Expression<Func<User, Department>> propertyPath = x => x.Department;
            Expression<Func<User, Department, bool>> condition = (x, y) => x.Id == 1;
            Expression<Func<User, Department, object>> selector = (x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name };

            var result = await memberDataAccess.QueryOneAsync<Department>((propertyPath, join, JoinType.Inner), condition, selector: selector);

            result.Name.Should().Be("Johnny");
            result.Department.DepId.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_InnerJoin_Two_Tables_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => x.Id == 1)
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .QueryOneAsync();

            result.Name.Should().Be("Johnny");
            result.Department.DepId.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_InnerJoin_Self_Two_Tables_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Self, (x, y) => x.Id == y.Id)
                             .Where((x, y) => x.Id == 1)
                             .Select((x, y) => new { x.Id, SelfId = y.Id, x.Name, SelfName = y.Name })
                             .QueryOneAsync();

            result.Id.Should().Be(1);
            result.Name.Should().Be("Johnny");
            result.Self.Id.Should().Be(1);
            result.Self.Name.Should().Be("Johnny");
        }

        [TestMethod]
        public void Test_QueryOneAsync_with_InnerJoin_Two_Tables_only_Left_will_Throw_ArgumentException()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            memberDataAccess
                .Invoking(
                    async dataAccess => await dataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                                            .Where((x, y) => x.Id == 1)
                                            .Select((x, y) => new { x.Id, x.Name })
                                            .QueryOneAsync())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id*");
        }

        [TestMethod]
        public void Test_QueryOne()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = clubDataAccess.QueryOne(x => x.Id == 25, null, x => new { x.Name });

            club.Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryOneAsync()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 25, null, x => new { x.Name });

            club.Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_Selector()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 25, selector: x => new { x.Name });

            club.Id.Should().Be(0);
            club.Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_Selector_use_QueryObject()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.Where(x => x.Id == 25).Select(x => new { x.Name }).QueryOneAsync();

            club.Id.Should().Be(0);
            club.Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_use_SelectAll()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.Where(x => x.Id == 35).SelectAll().QueryOneAsync();

            club.Id.Should().Be(35);
            club.Name.Should().Be("韓靜宜");
            club.IsActive.Should().BeTrue();
            club.Intro.Should().Be("韓");
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_use_SelectAll_with_NotMapped()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.Where(x => x.Id == 35).SelectAll().QueryOneAsync();

            club.Id.Should().Be(35);
            club.Name.Should().Be("韓靜宜");
            club.IsActive.Should().BeTrue();
            club.Intro.Should().Be("韓");
            club.IgnoreColumn.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_AS_Keyword_Alias()
        {
            var advertisementSettingDataAccess = DataAccessFactory.Create<AdvertisementSetting>("Advertisement");

            var result = await advertisementSettingDataAccess.Where(x => x.Type == "1000x90首頁下").SelectAll().QueryOneAsync();

            result.Id.Should().Be(Guid.Parse("df31efe5-b78f-4b4b-954a-0078328e34d2"));
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_assign_ConnectionString_Name()
        {
            var advertisementSettingDataAccess = DataAccessFactory.Create<AdvertisementSetting>("Advertisement2");

            var result = await advertisementSettingDataAccess.Where(x => x.Type == "1000x90首頁下").SelectAll().QueryOneAsync();

            result.Id.Should().Be(Guid.Parse("df31efe5-b78f-4b4b-954a-0078328e34d2"));
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_Multiple_ConnectionStringAttributes()
        {
            var advertisementSettingDataAccess = DataAccessFactory.Create<DerivedAdvertisementSetting>("Advertisement");

            var result = await advertisementSettingDataAccess.Where(x => x.Type == "1000x90首頁下").SelectAll().QueryOneAsync();

            result.Id.Should().Be(Guid.Parse("df31efe5-b78f-4b4b-954a-0078328e34d2"));
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_with_assign_ConnectionString_Name_from_DerivedClass()
        {
            var advertisementSettingDataAccess = DataAccessFactory.Create<DerivedAdvertisementSetting>("Advertisement3");

            var result = await advertisementSettingDataAccess.Where(x => x.Type == "1000x90首頁下").SelectAll().QueryOneAsync();

            result.Id.Should().Be(Guid.Parse("df31efe5-b78f-4b4b-954a-0078328e34d2"));
        }

        [TestMethod]
        public async Task Test_QueryOneAsync_use_this_Keyword()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.Where(x => x.Id == this.Club.Id).Select(x => new { x.Name }).QueryOneAsync();

            club.Id.Should().Be(0);
            club.Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public void Test_QueryOneAsync_without_ConnectionString_Name_in_Multiple_ConnectionStringAttribute_will_Throw_ArgumentException()
        {
            DataAccessFactory.Invoking(factory => factory.Create<AdvertisementSetting>())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Must indicate named connection string.");
        }

        [TestMethod]
        public void Test_QueryOneAsync_without_ConnectionString_will_Throw_ArgumentException()
        {
            DataAccessFactory.Invoking(factory => factory.Create<AnotherMember>())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Must add connection string.");
        }

        [TestMethod]
        public void Test_QueryAsync_use_Null_Selector_will_Throw_ArgumentException()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            clubDataAccess.Invoking(async dataAccess => await dataAccess.Where(x => x.Id == 35).QueryOneAsync())
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Must be at least one column selected.");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_InnerJoin_Two_Tables_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => new[] { 1, 2 }.Contains(x.Id))
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .QueryAsync();

            result.Count.Should().Be(2);
            result[0].Name.Should().Be("Johnny");
            result[0].Department.DepId.Should().Be(3);
            result[1].Name.Should().Be("Amy");
            result[1].Department.DepId.Should().Be(2);
        }

        [TestMethod]
        public async Task Test_QueryAsync_use_OrderBy_with_InnerJoin_Two_Tables_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => new[] { 1, 2 }.Contains(x.Id))
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .OrderBy((x, y) => y.DepId)
                             .QueryAsync();

            result.Count.Should().Be(2);
            result[0].Name.Should().Be("Amy");
            result[0].Department.DepId.Should().Be(2);
            result[1].Name.Should().Be("Johnny");
            result[1].Department.DepId.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_QueryAsync_use_And_with_InnerJoin_Two_Tables()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => new[] { 1, 2 }.Contains(x.Id))
                             .And((x, y) => y.DepId == 3)
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .QueryAsync();

            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Johnny");
            result[0].Department.DepId.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_InnerJoin_Two_Tables_and_No_Right_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.InnerJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => new[] { 1, 4 }.Contains(x.Id))
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .QueryAsync();

            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Johnny");
            result[0].Department.DepId.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_LeftJoin_Two_Tables_use_QueryObjet()
        {
            var memberDataAccess = DataAccessFactory.Create<User>();

            var result = await memberDataAccess.LeftJoin(x => x.Department, (x, y) => x.DepartmentId == y.DepId)
                             .Where((x, y) => new[] { 1, 4 }.Contains(x.Id))
                             .Select((x, y) => new { x.Id, y.DepId, x.Name, DepartmentName = y.Name })
                             .QueryAsync();

            result.Count.Should().Be(2);
            result[0].Name.Should().Be("Johnny");
            result[0].Department.DepId.Should().Be(3);
            result[1].Name.Should().Be("Flosser");
            result[1].Department.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Null()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => x.Intro == null).SelectAll().QueryAsync();

            clubs.Count.Should().BeGreaterOrEqualTo(5);
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.QueryAsync(x => new[] { 17, 25 }.Contains(x.Id), selector: x => new { x.Name });

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(0);
            clubs[1].Id.Should().Be(0);
            clubs[0].Name.Should().Be("吳淑娟");
            clubs[1].Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => new[] { 17, 25 }.Contains(x.Id)).Select(x => new { x.Name }).QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(0);
            clubs[1].Id.Should().Be(0);
            clubs[0].Name.Should().Be("吳淑娟");
            clubs[1].Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject_has_DateTime_Now()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => x.RunningTime < DateTime.Now).Select(x => new { x.Id, x.Name }).QueryAsync();

            clubs.Count.Should().Be(1);
            clubs[0].Id.Should().Be(39);
            clubs[0].Name.Should().Be("王真希");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject_and_And()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => x.RunningTime < DateTime.Now)
                            .And(y => y.IsActive == false)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject_and_Or()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => x.RunningTime > DateTime.Now)
                            .Or(y => y.Name == "王真希")
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(1);
            clubs[0].Id.Should().Be(39);
            clubs[0].Name.Should().Be("王真希");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject_and_OrderByDescending()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => new[] { 17, 25 }.Contains(x.Id))
                            .OrderByDescending(x => x.Id)
                            .Select(x => new { x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(0);
            clubs[1].Id.Should().Be(0);
            clubs[0].Name.Should().Be("鄧偉成");
            clubs[1].Name.Should().Be("吳淑娟");
        }

        [TestMethod]
        public async Task Test_QueryAsync_with_Selector_use_QueryObject_and_OrderByDescending_and_Top()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.Where(x => new[] { 17, 25 }.Contains(x.Id))
                            .OrderByDescending(x => x.Id)
                            .Select(x => new { x.Name })
                            .Top(1)
                            .QueryAsync();

            clubs.Count.Should().Be(1);
            clubs[0].Id.Should().Be(0);
            clubs[0].Name.Should().Be("鄧偉成");
        }

        [TestMethod]
        public async Task Test_QueryAllAsync_with_Selector_use_QueryObject_and_OrderByDescending_and_ThenBy_Top()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubs = await clubDataAccess.OrderByDescending(x => x.IsActive)
                            .ThenBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .Top(1)
                            .QueryAsync();

            clubs.Count.Should().Be(1);
            clubs[0].Id.Should().Be(9);
            clubs[0].Name.Should().Be("吳美惠");
        }

        [TestMethod]
        public async Task Test_CountAsync()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubCount = await clubDataAccess.Where(x => x.Intro == "陳").CountAsync();

            clubCount.Should().Be(3);
        }

        [TestMethod]
        public async Task Test_ExistsAsync()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var isExists = await clubDataAccess.Where(x => x.Id > 0).ExistsAsync();

            isExists.Should().BeTrue();

            isExists = await clubDataAccess.Where(x => x.Id < 0).ExistsAsync();

            isExists.Should().BeFalse();
        }

        [TestMethod]
        public async Task Test_UpdateAsync()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubName = "歐陽邦瑋" + suffix;

            await clubDataAccess.UpdateAsync(x => x.Id.Equals(15), () => new Club { Name = clubName });

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 15, null, x => new { x.Id, x.Name });

            club.Id.Should().Be(15);
            club.Name.Should().Be("歐陽邦瑋" + suffix);
        }

        [TestMethod]
        public void Test_UpdateAsync_use_NotMapped_Column_will_Throw_ArgumentException()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubName = "歐陽邦瑋" + suffix;

            clubDataAccess
                .Invoking(
                    async dataAccess => await dataAccess.UpdateAsync(
                                            x => x.Id.Equals(15),
                                            () => new Club { Name = clubName, IgnoreColumn = "testabc" }))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Member can not applied [NotMapped].");
        }

        [TestMethod]
        public async Task Test_UpdateAsync_set_Null()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 36, selector: x => new { x.Id, x.Intro });

            club.Id.Should().Be(36);
            club.Intro.Should().Be("連");

            await clubDataAccess.UpdateAsync(x => x.Id.Equals(36), () => new Club { Intro = null });

            club = await clubDataAccess.QueryOneAsync(x => x.Id == 36, selector: x => new { x.Id, x.Intro });

            await clubDataAccess.UpdateAsync(x => x.Id.Equals(36), () => new Club { Intro = "連" });

            club.Id.Should().Be(36);
            club.Intro.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_UpdateAsync_use_QueryObject()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubDataAccess = DataAccessFactory.Create<Club>();

            var clubName = "歐陽邦瑋" + suffix;

            await clubDataAccess.Where(x => x.Id.Equals(15)).Set(() => new Club { Name = clubName }).UpdateAsync();

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 15, null, x => new { x.Id, x.Name });

            club.Id.Should().Be(15);
            club.Name.Should().Be("歐陽邦瑋" + suffix);
        }

        [TestMethod]
        public async Task Test_UpdateAsync_use_QueryObject_set_Null()
        {
            var clubDataAccess = DataAccessFactory.Create<Club>();

            var club = await clubDataAccess.QueryOneAsync(x => x.Id == 36, selector: x => new { x.Id, x.Intro });

            club.Id.Should().Be(36);
            club.Intro.Should().Be("連");

            await clubDataAccess.Where(x => x.Id == 36).Set(() => new Club { Intro = null }).UpdateAsync();

            club = await clubDataAccess.Where(x => x.Id == 36).Select(x => new { x.Id, x.Intro }).QueryOneAsync();

            await clubDataAccess.Where(x => x.Id == 36).Set(() => new Club { Intro = "連" }).UpdateAsync();

            club.Id.Should().Be(36);
            club.Intro.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_UpdateAsync_Multiply()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubs = new List<Club>
                        {
                            new Club { Id = 15, Name = "歐陽邦瑋" + suffix },
                            new Club { Id = 16, Name = "羅怡君" + suffix },
                            new Club { Id = 19, Name = "楊翊貴" + suffix }
                        };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.UpdateAsync(x => x.Id == default(int), () => new Club { Name = default(string) }, clubs);

            var actual = await clubDataAccess.QueryAsync(x => new[] { 15, 16, 19 }.Contains(x.Id), selector: x => new { x.Id, x.Name });

            actual.Single(x => x.Id.Equals(15)).Name.Should().Be("歐陽邦瑋" + suffix);
            actual.Single(x => x.Id.Equals(16)).Name.Should().Be("羅怡君" + suffix);
            actual.Single(x => x.Id.Equals(19)).Name.Should().Be("楊翊貴" + suffix);
        }

        [TestMethod]
        public async Task Test_UpdateAsync_Multiply_use_QueryObject()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubs = new List<Club>
                        {
                            new Club { Id = 15, Name = "歐陽邦瑋" + suffix },
                            new Club { Id = 16, Name = "羅怡君" + suffix },
                            new Club { Id = 19, Name = "楊翊貴" + suffix }
                        };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Where(x => x.Id == default(int)).Set(() => new Club { Name = default(string) }).UpdateAsync(clubs);

            var actual = await clubDataAccess.QueryAsync(x => new[] { 15, 16, 19 }.Contains(x.Id), selector: x => new { x.Id, x.Name });

            actual.Single(x => x.Id.Equals(15)).Name.Should().Be("歐陽邦瑋" + suffix);
            actual.Single(x => x.Id.Equals(16)).Name.Should().Be("羅怡君" + suffix);
            actual.Single(x => x.Id.Equals(19)).Name.Should().Be("楊翊貴" + suffix);
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(new Club { Id = clubId, Name = "TestClub" });

            var club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub");

            club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            club.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync_use_Setter()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(() => new Club { Id = clubId, Name = "TestClub" });

            var club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub");

            club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            club.Should().BeNull();
        }

        [TestMethod]
        public void Test_InsertAsync_use_Setter_Has_NotMapped_Column_will_Throw_ArgumentException()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            clubDataAccess
                .Invoking(
                    async dataAccess =>
                        await dataAccess.InsertAsync(() => new Club { Id = clubId, Name = "TestClub", IgnoreColumn = "testabc" }))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Member can not applied [NotMapped].");
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync_use_QueryObject()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Set(() => new Club { Id = clubId, Name = "TestClub" }).InsertAsync();

            var club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            await clubDataAccess.Where(x => x.Id == clubId).DeleteAsync();

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub");

            club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            club.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync_Multiply()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(
                new List<Club>
                {
                    new Club { Id = clubIds[1], Name = "TestClub999", IsActive = true }, new Club { Id = clubIds[0], Name = "TestClub998" }
                });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name, x.IsActive })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(clubIds[0]);
            clubs[0].IsActive.Should().BeFalse();
            clubs[1].IsActive.Should().BeTrue();
            clubs[1].Name.Should().Be("TestClub999");

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).QueryAsync();

            clubs.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync_Multiply_use_Setter()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(
                () => new Club { Id = default(int), Name = default(string) },
                new List<Club> { new Club { Id = clubIds[1], Name = "TestClub999" }, new Club { Id = clubIds[0], Name = "TestClub998" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(clubIds[0]);
            clubs[1].Name.Should().Be("TestClub999");

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).QueryAsync();

            clubs.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Test_InsertAsync_and_DeleteAsync_Multiply_use_Setter_and_QueryObject()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Set(() => new Club { Id = default(int), Name = default(string) })
                .InsertAsync(new List<Club> { new Club { Id = clubIds[1], Name = "TestClub999" }, new Club { Id = clubIds[0], Name = "TestClub998" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Id.Should().Be(clubIds[0]);
            clubs[1].Name.Should().Be("TestClub999");

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).QueryAsync();

            clubs.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Test_UpsertAsync()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.UpsertAsync(x => x.Id == clubId, () => new Club { Name = "TestClub" });

            var club = await clubDataAccess.Where(x => x.Id == clubId)
                            .Select(x => new { x.Id, x.Name, x.IsActive })
                            .QueryOneAsync();

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub");
            club.IsActive.Should().BeTrue();

            await clubDataAccess.UpsertAsync(
                x => x.Id == clubId && x.IsActive == true,
                () => new Club { Name = "TestClub997", IsActive = false });

            club = await clubDataAccess.Where(x => x.Id == clubId)
                       .Select(x => new { x.Id, x.Name, x.IsActive })
                       .QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub997");
            club.IsActive.Should().BeFalse();

            club = await clubDataAccess.Where(x => x.Id == clubId)
                       .Select(x => new { x.Id, x.Name, x.IsActive })
                       .QueryOneAsync();

            club.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_UpsertAsync_use_QueryObject()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Where(x => x.Id == clubId).Set(() => new Club { Name = "TestClub" }).UpsertAsync();

            var club = await clubDataAccess.Where(x => x.Id == clubId)
                            .Select(x => new { x.Id, x.Name, x.IsActive })
                            .QueryOneAsync();

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub");
            club.IsActive.Should().BeTrue();

            await clubDataAccess.Where(x => x.Id == clubId && x.IsActive == true)
                .Set(() => new Club { Name = "TestClub997", IsActive = false })
                .UpsertAsync();

            club = await clubDataAccess.Where(x => x.Id == clubId)
                       .Select(x => new { x.Id, x.Name, x.IsActive })
                       .QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Id.Should().Be(clubId);
            club.Name.Should().Be("TestClub997");
            club.IsActive.Should().BeFalse();

            club = await clubDataAccess.Where(x => x.Id == clubId)
                       .Select(x => new { x.Id, x.Name, x.IsActive })
                       .QueryOneAsync();

            club.Should().BeNull();
        }

        [TestMethod]
        public async Task Test_UpsertAsync_Multiply()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.UpsertAsync(
                x => x.Id == default(int),
                () => new Club { Name = default(string) },
                new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");

            await clubDataAccess.UpsertAsync(
                x => x.Id == default(int),
                () => new Club { Name = default(string) },
                new List<Club> { new Club { Id = clubIds[0], Name = "TestClub3" }, new Club { Id = clubIds[1], Name = "TestClub4" } });

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Select(x => new { x.Id, x.Name })
                        .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub3");
            clubs[1].Name.Should().Be("TestClub4");
        }

        [TestMethod]
        public async Task Test_UpsertAsync_Multiply_use_QueryObject()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Where(x => x.Id == default(int))
                .Set(() => new Club { Name = default(string) })
                .UpsertAsync(
                    new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");

            await clubDataAccess.Where(x => x.Id == default(int))
                .Set(() => new Club { Name = default(string) })
                .UpsertAsync(
                    new List<Club> { new Club { Id = clubIds[0], Name = "TestClub3" }, new Club { Id = clubIds[1], Name = "TestClub4" } });

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Select(x => new { x.Id, x.Name })
                        .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub3");
            clubs[1].Name.Should().Be("TestClub4");
        }

        [TestMethod]
        public async Task Test_BulkInsert()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.BulkInsertAsync(
                () => new Club { Id = default(int), Name = default(string) },
                new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");
        }

        [TestMethod]
        public void Test_BulkInsert_without_UserDefinedTable_will_Throw_ArgumentException()
        {
            var advertisementSettingDataAccess = DataAccessFactory.Create<AdvertisementSetting>("Advertisement");

            advertisementSettingDataAccess
                .Invoking(
                    async dataAccess => await advertisementSettingDataAccess.BulkInsertAsync(
                                            () => new AdvertisementSetting { Id = default(Guid) },
                                            new List<AdvertisementSetting>()))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("Must add UserDefinedTableType.");
        }

        [TestMethod]
        public async Task Test_BulkInsert_use_QueryObject()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Set(() => new Club { Id = default(int), Name = default(string) })
                .BulkInsertAsync(
                    new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");
        }

        [TestMethod]
        public async Task Test_BulkInsert_use_QueryObject_with_RequiredColumns()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.BulkInsertAsync(
                new List<Club>
                {
                    new Club { Id = clubIds[0], Name = "TestClub1", IsActive = false },
                    new Club { Id = clubIds[1], Name = "TestClub2", IsActive = true }
                });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name, x.IsActive })
                            .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");
            clubs[0].IsActive.Should().BeFalse();
            clubs[1].IsActive.Should().BeTrue();
        }

        [TestMethod]
        public async Task Test_BulkUpdate()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubs = new List<Club>
                        {
                            new Club { Id = 15, Name = "歐陽邦瑋" + suffix },
                            new Club { Id = 16, Name = "羅怡君" + suffix },
                            new Club { Id = 19, Name = "楊翊貴" + suffix }
                        };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.BulkUpdateAsync(x => x.Id == default(int), () => new Club { Name = default(string) }, clubs);

            var actual = await clubDataAccess.QueryAsync(x => new[] { 15, 16, 19 }.Contains(x.Id), selector: x => new { x.Id, x.Name });

            actual.Single(x => x.Id.Equals(15)).Name.Should().Be("歐陽邦瑋" + suffix);
            actual.Single(x => x.Id.Equals(16)).Name.Should().Be("羅怡君" + suffix);
            actual.Single(x => x.Id.Equals(19)).Name.Should().Be("楊翊貴" + suffix);
        }

        [TestMethod]
        public async Task Test_BulkUpdate_use_QueryObject()
        {
            var suffix = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000).ToString();

            var clubs = new List<Club>
                        {
                            new Club { Id = 15, Name = "歐陽邦瑋" + suffix },
                            new Club { Id = 16, Name = "羅怡君" + suffix },
                            new Club { Id = 19, Name = "楊翊貴" + suffix }
                        };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Where(x => x.Id == default(int)).Set(() => new Club { Name = default(string) }).BulkUpdateAsync(clubs);

            var actual = await clubDataAccess.QueryAsync(x => new[] { 15, 16, 19 }.Contains(x.Id), selector: x => new { x.Id, x.Name });

            actual.Single(x => x.Id.Equals(15)).Name.Should().Be("歐陽邦瑋" + suffix);
            actual.Single(x => x.Id.Equals(16)).Name.Should().Be("羅怡君" + suffix);
            actual.Single(x => x.Id.Equals(19)).Name.Should().Be("楊翊貴" + suffix);
        }

        [TestMethod]
        public async Task Test_BulkUpsertAsync()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.BulkUpsertAsync(
                x => x.Id >= 0 && x.Id <= 0,
                () => new Club { Name = default(string) },
                new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");

            await clubDataAccess.BulkUpsertAsync(
                x => x.Id == default(int),
                () => new Club { Name = default(string) },
                new List<Club> { new Club { Id = clubIds[0], Name = "TestClub3" }, new Club { Id = clubIds[1], Name = "TestClub4" } });

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Select(x => new { x.Id, x.Name })
                        .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub3");
            clubs[1].Name.Should().Be("TestClub4");
        }

        [TestMethod]
        public async Task Test_BulkUpsertAsync_use_QueryObject()
        {
            var clubIds = new[]
                          {
                              new Random(Guid.NewGuid().GetHashCode()).Next(100, 500),
                              new Random(Guid.NewGuid().GetHashCode()).Next(500, 1000)
                          };

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.Where(x => x.Id >= default(int) && x.Id <= default(int))
                .Set(() => new Club { Name = default(string) })
                .BulkUpsertAsync(
                    new List<Club> { new Club { Id = clubIds[0], Name = "TestClub1" }, new Club { Id = clubIds[1], Name = "TestClub2" } });

            var clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .Select(x => new { x.Id, x.Name })
                            .QueryAsync();

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub1");
            clubs[1].Name.Should().Be("TestClub2");

            await clubDataAccess.Where(x => x.Id == default(int))
                .Set(() => new Club { Name = default(string) })
                .BulkUpsertAsync(
                    new List<Club> { new Club { Id = clubIds[0], Name = "TestClub3" }, new Club { Id = clubIds[1], Name = "TestClub4" } });

            clubs = await clubDataAccess.Where(x => clubIds.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Select(x => new { x.Id, x.Name })
                        .QueryAsync();

            await clubDataAccess.DeleteAsync(x => clubIds.Contains(x.Id));

            clubs.Count.Should().Be(2);
            clubs[0].Name.Should().Be("TestClub3");
            clubs[1].Name.Should().Be("TestClub4");
        }

        [TestMethod]
        public async Task Test_TransactionScope_Query_and_Update()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(() => new Club { Id = clubId, Name = "TestClub" });

            Club club;
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

                club.Name += "989";

                await clubDataAccess.Where(x => x.Id == clubId).Set(() => new Club { Name = club.Name }).UpdateAsync();

                tx.Complete();
            }

            club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Name.Should().Be("TestClub989");
        }

        [TestMethod]
        public async Task Test_TransactionScope_Multiple_Query_and_Update()
        {
            var clubId = new Random(Guid.NewGuid().GetHashCode()).Next(100, 1000);

            var clubDataAccess = DataAccessFactory.Create<Club>();

            await clubDataAccess.InsertAsync(() => new Club { Id = clubId, Name = "TestClub" });

            Club club;
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

                club.Name += "979";

                await clubDataAccess.Where(x => x.Id == default(int))
                    .Set(() => new Club { Name = default(string) })
                    .UpdateAsync(new List<Club> { new Club { Id = clubId, Name = club.Name } });

                tx.Complete();
            }

            club = await clubDataAccess.Where(x => x.Id == clubId).Select(x => new { x.Id, x.Name }).QueryOneAsync();

            await clubDataAccess.DeleteAsync(x => x.Id == clubId);

            club.Name.Should().Be("TestClub979");
        }
    }

    [ConnectionString(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=Club;Integrated Security=True")]
    internal class Club
    {
        [Column("ClubID")]
        [Required]
        public int Id { get; set; }

        [StringLength(50)]
        [Required]
        public string Name { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public string Intro { get; set; }

        public DateTime? RunningTime { get; set; }

        [NotMapped]
        public string IgnoreColumn { get; set; }
    }

    [ConnectionString("Advertisement")]
    [ConnectionString("Advertisement2")]
    internal class AdvertisementSetting
    {
        public string Type { get; set; }

        public Guid Id { get; set; }

        public string Image { get; set; }

        public string Link { get; set; }

        public int Weight { get; set; }

        public string AdCode { get; set; }
    }

    [Table("Member")]
    internal class AnotherMember
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }
    }

    [ConnectionString(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=Member;Integrated Security=True")]
    [Table("Member")]
    internal class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public int DepartmentId { get; set; }

        public Department Department { get; set; }

        public User Self { get; set; }
    }

    internal class Department
    {
        [Column("Id")]
        public int DepId { get; set; }

        public string Name { get; set; }
    }

    [ConnectionString("Advertisement3")]
    [Table("AdvertisementSetting")]
    internal class DerivedAdvertisementSetting : AdvertisementSetting
    {
    }
}
