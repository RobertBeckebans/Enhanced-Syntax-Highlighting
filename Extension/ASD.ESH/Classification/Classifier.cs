﻿// Copyright (c) "ESH-Repository" source code contributors. All Rights Reserved.
// Licensed under the Microsoft Public License (MS-PL).
// See LICENSE.md in the "ESH-Repository" root for license information.
// "ESH-Repository" root address: https://github.com/Art-Stea1th/Enhanced-Syntax-Highlighting

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace ASD.ESH.Classification {

    using Helpers;

    internal sealed partial class Classifier : IClassifier {

#pragma warning disable CS0067
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore CS0067

        private static readonly IList<ClassificationSpan> emptyList = new List<ClassificationSpan>(capacity: 0).AsReadOnly();

        private Document document = null;
        private ITextSnapshot snapshot = null;
        private IList<ClassificationSpan> lastSpans = emptyList;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {

            var snapshot = span.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document == null) {
                return emptyList;
            }
            if (this.document?.Id == document.Id && this.snapshot == snapshot) {
                return lastSpans;
            }

            this.document = document;
            this.snapshot = snapshot;

            return lastSpans = Task.Run(() => GetSpansAync(document, snapshot)).GetAwaiter().GetResult();
        }

        private async Task<IList<ClassificationSpan>> GetSpansAync(Document document, ITextSnapshot snapshot) {

            var modelTask = document.GetSemanticModelAsync();
            var rootTask = document.GetSyntaxRootAsync();
            var spansTask = document.GetClassifiedSpansAsync(new TextSpan(0, snapshot.Length));

            await Task.WhenAll(modelTask, rootTask, spansTask);

            var model = modelTask.GetAwaiter().GetResult();
            var root = rootTask.GetAwaiter().GetResult();
            var spans = spansTask.GetAwaiter().GetResult();

            var resultSpans = new List<ClassificationSpan>(spans.Count());
            var converter = new SpansConverter(model, root, snapshot);

            foreach (var span in converter.ConvertAll(spans)) {
                resultSpans.Add(span);
            }
            return resultSpans;
        }
    }
}