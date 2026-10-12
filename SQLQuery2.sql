SELECT 
    (SELECT COUNT(*) FROM Staffs) AS StaffCount,
    (SELECT COUNT(*) FROM AspNetUsers) AS UserCount,
    (SELECT COUNT(*) FROM Projects) AS ProjectCount;