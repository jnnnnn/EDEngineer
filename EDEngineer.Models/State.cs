﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EDEngineer.Models.Utils.Collections;

namespace EDEngineer.Models
{
    public class State : INotifyPropertyChanged
    {
        public LinkedList<JournalEntry> Operations { get; } = new LinkedList<JournalEntry>();

        private readonly List<EntryData> entryDatas;

        private readonly object stateLock = new object();
        public List<Blueprint> Blueprints { get; set; }

        public State(List<EntryData> entryDatas, ILanguage languages)
        {
            Cargo = new SortedObservableCounter((a, b) => string.Compare(languages.Translate(a.Key), languages.Translate(b.Key), StringComparison.InvariantCultureIgnoreCase));
            languages.PropertyChanged += (o, e) => Cargo.RefreshSort();
            
            this.entryDatas = entryDatas;
            LoadBaseData();
        }

        public SortedObservableCounter Cargo { get; }

        public int MaterialsCount => EntryCount(Kind.Material);

        public int DataCount => EntryCount(Kind.Data);

        public int MaxMaterials { get; } = 1000;
        public int MaxData { get; } = 500;

        private int EntryCount(Kind kind)
        {
            return Cargo
                .Where(i => i.Value.Data.Kind == kind)
                .Where(i => i.Value.Count > 0)
                .Select(i => i.Value.Count)
                .Sum();
        }

        public void LoadBaseData()
        {
            lock (stateLock)
            {
                var toAdd = entryDatas.Where(e => !Cargo.ContainsKey(e.Name));
                foreach (var item in toAdd)
                {
                    Cargo.Add(new KeyValuePair<string, Entry>(item.Name, new Entry(item)));
                }
            }
        }

        public void IncrementCargo(string name, int change)
        {
            lock (stateLock)
            {
                Cargo.Increment(name, change);
            }

            switch (Cargo[name].Data.Kind)
            {
                case Kind.Data:
                    OnPropertyChanged(nameof(DataCount));
                    break;
                case Kind.Material:
                    OnPropertyChanged(nameof(MaterialsCount));
                    break;
            }

            OnPropertyChanged(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}