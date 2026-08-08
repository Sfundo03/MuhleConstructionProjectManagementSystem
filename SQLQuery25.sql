-- ============================================
-- DELETE ALL QUIZ AND INTERVIEW DATA
-- Run in order (child tables first)
-- ============================================

-- 1. Delete Quiz Attempts (child of Quiz and JobApplication)
DELETE FROM QuizAttempts;
PRINT 'Deleted QuizAttempts';

-- 2. Delete Quiz Questions (child of Quiz)
DELETE FROM QuizQuestions;
PRINT 'Deleted QuizQuestions';

-- 3. Delete Quizzes
DELETE FROM Quizzes;
PRINT 'Deleted Quizzes';

-- 4. Clear Quiz data from JobApplications
UPDATE JobApplications 
SET QuizScore = NULL, 
    QuizPassed = NULL, 
    QuizCompletedDate = NULL;
PRINT 'Cleared Quiz data from JobApplications';

-- 5. Clear Interview data from JobApplications
UPDATE JobApplications 
SET InterviewScheduled = 0, 
    InterviewDateTime = NULL, 
    InterviewNotes = NULL, 
    InterviewResult = 'Pending', 
    InterviewFeedback = NULL, 
    InterviewAssessedBy = NULL, 
    InterviewAssessedDate = NULL,
    IsShortlisted = 0,
    ShortlistDate = NULL,
    Status = 'Pending';
PRINT 'Cleared Interview data from JobApplications';

-- 6. Delete Application Status History related to interviews and quizzes
DELETE FROM ApplicationStatusHistories 
WHERE Status IN ('Interview Scheduled', 'Quiz Passed', 'Quiz Failed', 'Passed Interview', 'Failed Interview')
   OR Notes LIKE '%quiz%' 
   OR Notes LIKE '%interview%';
PRINT 'Deleted ApplicationStatusHistories';

-- 7. Delete Notifications related to quizzes and interviews
DELETE FROM Notifications 
WHERE Type IN ('Quiz', 'Interview') 
   OR Title LIKE '%Quiz%' 
   OR Title LIKE '%Interview%';
PRINT 'Deleted Notifications';