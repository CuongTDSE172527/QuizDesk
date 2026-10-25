using QuizDesk;

partial class Tests {
    static void StudyChecks(string folder,string document) {
        Directory.CreateDirectory(folder);
        var bank=Reader.Parse("Câu 1. Trái cây?\nA. Táo\nB. Lê\nĐáp án gốc: A. Táo\nSau rà soát: B — sửa theo tài liệu.\nGiải thích: Lý do riêng.\nCâu 2. Chưa rõ?\nA. Một\nB. Hai\nĐáp án gốc: A\nSau rà soát: CHƯA CHỐT — cần kiểm tra.\nGiải thích: Cần giáo trình.\nNguồn đối chiếu\nKhông được gắn vào câu cuối.");
        Check(bank.Count==2&&bank[0].Answer=="B"&&bank[0].OriginalAnswer=="A. Táo","Reviewed inline key supersedes original while preserving provenance");
        Check(bank[0].Note.Contains("Lý do riêng")&&!bank[0].Options.Values.Any(v=>v.Contains("rà soát")),"Review metadata stays out of choices");
        Check(bank[1].Answer==""&&QuestionReview.RequiresReview(bank[1])&&bank[1].OriginalAnswer=="A","Unresolved verdict never falls back to original");
        Check(!bank[1].Note.Contains("Không được gắn"),"References are not appended to last explanation");
        var repeated=Reader.Parse("Câu 1. Chọn?\nA. Một\nB. Hai\nĐáp án gốc: A\nSau rà soát: A — giữ\nSau rà soát: B — sửa").Single();
        Check(QuestionReview.ScoringAnswer(repeated)==""&&repeated.OriginalAnswer=="A","Repeated reviewed claims remain unscored");
        var generic=Reader.Parse("Câu 1. Chọn?\nA. Một\nB. Hai\nĐáp án gốc: B. Hai").Single();
        Check(generic.Answer=="B","Original-only key still imports");
        var setup=new QuizSetup {Included=new(){1,2},ShuffleQuestions=false};
        var study=FlashcardSession.Create("Test",Guid.NewGuid(),bank,setup);
        Check(study.Cards.Count==1&&study.Excluded==1,"Flashcards exclude unconfirmed keys");
        Check(study.Current.Front=="Trái cây?"&&study.Current.Back=="B. Lê"&&study.Current.Note.Contains("Lý do riêng"),"Flashcard front/back/explanation separated");
        study.Rate(true);Check(study.Known.Count==0,"Rating before reveal is ignored");
        study.Flip();study.Rate(false);Check(study.Summary&&study.Known[0]==false,"Last card rating ends round");
        study.Repeat(false);Check(!study.Summary&&!study.Revealed&&study.Order.Count==1,"Repeat missed returns hidden front");
        study.Flip();study.Rate(true);Check(study.Remembered==1,"Learning card can become remembered");
        study.Repeat(false);study.Validate();Check(study.Summary&&study.Order.Count>0,"All-known repeat remains valid for persistence");
        study.Repeat(true);Check(study.Known.Count==0&&!study.Summary,"Full reset clears ratings");
        var store=new FlashcardStore(folder);study.Flip();store.Save(study);var restored=new FlashcardStore(folder).Load(out string warning);
        Check(warning==""&&restored.Revealed&&restored.Current.Back=="B. Lê"&&restored.BankId==study.BankId,"Flashcard snapshot/revealed state/bank identity survives restart");
        byte[] snapshot=File.ReadAllBytes(Path.Combine(folder,"flashcards.json"));study.Flip();store.Save(study);
        Check(snapshot.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"flashcards.json")))&&!new FlashcardStore(folder).Load(out _).Revealed,"Flips checkpoint progress without rewriting question snapshot");
        bank[0].Options["B"]="Changed later";Check(study.Current.Back=="B. Lê","Study snapshot is independent of later edits");
        bool empty=false;try{FlashcardSession.Create("Empty",null,bank,new(){Included=new(){2}});}catch(InvalidOperationException){empty=true;}Check(empty,"All-unconfirmed selection gives useful empty result");
        File.WriteAllText(Path.Combine(folder,"flashcards.json"),"{broken");Check(store.Load(out warning)==null&&warning!="","Corrupt progress is reported without crashing or deleting source");
        if(document!=null){
            var questions=Importers.Load(document,out var config);
            string[] expected={"D","D","D","B","A","A","B","C","D","","","E","C","E","E","","E","A","C","C","E","D","D","B","A","","A","C","B",""};
            Check(questions.Count==30,"Real Mon15 document imports all 30 questions");
            for(int i=0;i<30;i++)Check(questions[i].Answer==expected[i],"Mon15 reviewed key "+(i+1));
            Check(questions.All(q=>q.OriginalAnswer!=""&&q.Note.Contains("Sau rà soát:")),"All original keys, review decisions and explanations retained");
            Check(questions.All(q=>!q.Options.Values.Any(s=>s.Contains("Đáp án gốc:")||s.Contains("Sau rà soát:"))),"No answer metadata leaks into distractors");
            Check(!questions.Last().Note.Contains("Nguồn đối chiếu")&&!questions.Last().Note.Contains("https://"),"Bibliography not merged into final answer");
            var flash=FlashcardSession.Create("Môn 15",null,questions,config);
            Check(flash.Cards.Count==25&&flash.Excluded==5,"Mon15 creates exactly 25 confirmed cards, excludes five unresolved");
            int learned=flash.Order[1];flash.Flip();flash.Rate(false);flash.Flip();flash.Rate(true);flash.Position=24;flash.Flip();flash.Rate(false);
            flash.Repeat(false);Check(flash.Order.Count==24&&!flash.Order.Contains(learned),"Missed/unrated repeat excludes remembered cards");
            QuizFile.Save(Path.Combine(folder,"Mon-15-da-nhan-dien.quiz"),questions,config);
            Check(Importers.Load(Path.Combine(folder,"Mon-15-da-nhan-dien.quiz"),out _).Select(q=>q.Answer).SequenceEqual(expected),"Reviewed keys survive quiz export/import");
        }
        Console.WriteLine("TOTAL "+checks+" study/reviewed-document checks passed.");
    }
}
