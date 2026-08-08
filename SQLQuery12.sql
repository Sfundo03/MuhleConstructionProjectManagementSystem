-- Check remaining data
SELECT 'Quizzes' as TableName, COUNT(*) as Count FROM Quizzes
UNION ALL
SELECT 'QuizQuestions', COUNT(*) FROM QuizQuestions
UNION ALL
SELECT 'QuizAttempts', COUNT(*) FROM QuizAttempts
UNION ALL
SELECT 'JobApplications with Interview', COUNT(*) FROM JobApplications WHERE InterviewScheduled = 1
UNION ALL
SELECT 'JobApplications with Quiz', COUNT(*) FROM JobApplications WHERE QuizScore IS NOT NULL;