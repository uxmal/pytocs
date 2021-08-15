#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    [Flags]
    public enum MemberAttributes
    {
        Abstract = 1,
        Final = 2,
        Static = 3,
        Override = 4,
        Const = 5,
        ScopeMask = 15,
        
        New = 16,
        VTableMask = 240,
        Overloaded = 256,
        Assembly = 4096,
        FamilyAndAssembly = 8192,
        Family = 12288,
        FamilyOrAssembly = 16384,
        
        Private = 20480,
        Public = 24576,
        AccessMask = 61440,
    }
}
