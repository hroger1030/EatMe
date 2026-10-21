using EatMeServer;

namespace EatMeTests
{
    public class ConsoleInputTests
    {
        private static ConsoleKeyInfo Key(ConsoleKey key, char c = '\0')
        {
            return new ConsoleKeyInfo(c, key, false, false, false);
        }

        private static ConsoleInput Typed(string text)
        {
            var input = new ConsoleInput();

            foreach (var c in text)
                input.Insert(c);

            return input;
        }

        [Test]
        public void TypedCharactersGoAtTheCursor()
        {
            var input = Typed("hlp");
            input.MoveLeft();
            input.MoveLeft();
            input.Insert('e');

            Assert.That(input.Text, Is.EqualTo("help"));
            Assert.That(input.Cursor, Is.EqualTo(2));
        }

        [Test]
        public void BackspaceRemovesBeforeTheCursorAndDeleteUnderIt()
        {
            var input = Typed("abcd");
            input.Home();
            input.Backspace();
            input.Delete();

            Assert.That(input.Text, Is.EqualTo("bcd"));

            input.End();
            input.Delete();
            input.Backspace();

            Assert.That(input.Text, Is.EqualTo("bc"));
            Assert.That(input.Cursor, Is.EqualTo(2));
        }

        [Test]
        public void CursorStaysInTheLine()
        {
            var input = Typed("ab");
            input.MoveRight();
            Assert.That(input.Cursor, Is.EqualTo(2));

            input.Home();
            input.MoveLeft();
            Assert.That(input.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void EnterReturnsTheLineAndEmptiesIt()
        {
            var input = Typed("kick 3");

            Assert.That(input.HandleKey(Key(ConsoleKey.Enter, '\r'), out var line), Is.True);
            Assert.That(line, Is.EqualTo("kick 3"));
            Assert.That(input.Text, Is.Empty);
            Assert.That(input.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void KeysEditTheLine()
        {
            var input = new ConsoleInput();

            foreach (var c in "lvel")
                Assert.That(input.HandleKey(Key(ConsoleKey.A, c), out _), Is.False);

            input.HandleKey(Key(ConsoleKey.Home), out _);
            input.HandleKey(Key(ConsoleKey.RightArrow), out _);
            input.HandleKey(Key(ConsoleKey.E, 'e'), out _);
            Assert.That(input.Text, Is.EqualTo("level"));

            input.HandleKey(Key(ConsoleKey.Tab, '\t'), out _);
            Assert.That(input.Text, Is.EqualTo("level"));

            input.HandleKey(Key(ConsoleKey.Escape, '\u001b'), out _);
            Assert.That(input.Text, Is.Empty);
        }

        [Test]
        public void UpAndDownWalkTheHistoryAndBringBackTheDraft()
        {
            var input = new ConsoleInput();

            foreach (var line in new[] { "status", "help" })
            {
                foreach (var c in line)
                    input.Insert(c);

                input.Submit();
            }

            input.Insert('x');
            input.HistoryUp();
            Assert.That(input.Text, Is.EqualTo("help"));
            Assert.That(input.Cursor, Is.EqualTo(4));

            input.HistoryUp();
            input.HistoryUp();
            Assert.That(input.Text, Is.EqualTo("status"));

            input.HistoryDown();
            Assert.That(input.Text, Is.EqualTo("help"));

            input.HistoryDown();
            Assert.That(input.Text, Is.EqualTo("x"));

            input.HistoryDown();
            Assert.That(input.Text, Is.EqualTo("x"));
        }

        [Test]
        public void HistorySkipsBlankAndRepeatedLines()
        {
            var input = new ConsoleInput();

            foreach (var line in new[] { "help", "help", "   ", "" })
            {
                foreach (var c in line)
                    input.Insert(c);

                input.Submit();
            }

            Assert.That(input.History, Is.EqualTo(new[] { "help" }));
        }

        [Test]
        public void HistoryKeepsTheNewestLines()
        {
            var input = new ConsoleInput();

            for (var i = 0; i < ConsoleInput.MAX_HISTORY + 5; i++)
            {
                foreach (var c in i.ToString())
                    input.Insert(c);

                input.Submit();
            }

            Assert.That(input.History.Count, Is.EqualTo(ConsoleInput.MAX_HISTORY));
            Assert.That(input.History[0], Is.EqualTo("5"));
        }

        [TestCase(5, 5, 10, 0)]
        [TestCase(9, 9, 10, 0)]
        [TestCase(10, 10, 10, 1)]
        [TestCase(20, 20, 10, 11)]
        [TestCase(20, 0, 10, 0)]
        [TestCase(20, 12, 10, 3)]
        public void ViewKeepsTheCursorInSight(int length, int cursor, int room, int expected)
        {
            Assert.That(ConsoleInput.GetViewStart(length, cursor, room), Is.EqualTo(expected));
        }

        [Test]
        public void PinnedPromptCodes()
        {
            Assert.That(ServerLog.GetScrollRegionCode(30), Is.EqualTo("\u001b[1;29r"));
            Assert.That(ServerLog.GetMoveCode(30, 12), Is.EqualTo("\u001b[30;12H"));
        }
    }
}
