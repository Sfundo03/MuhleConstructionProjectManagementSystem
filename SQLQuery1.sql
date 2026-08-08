-- Delete all job applications first (due to foreign key constraints)
DELETE FROM ApplicationStatusHistories;
DELETE FROM QuizAttempts;
DELETE FROM JobApplications;

-- Then delete all job posts
DELETE FROM JobPosts;

-- If you want to reset identity seed (optional)
DBCC CHECKIDENT ('JobPosts', RESEED, 0);
DBCC CHECKIDENT ('JobApplications', RESEED, 0);
DBCC CHECKIDENT ('ApplicationStatusHistories', RESEED, 0);