﻿//  Copyright (c) 2008 - 2010, www.metabolt.net
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  * Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
//  * Redistributions in binary form must reproduce the above copyright notice, 
//    this list of conditions and the following disclaimer in the documentation 
//    and/or other materials provided with the distribution. 
//  * Neither the name METAbolt nor the names of its contributors may be used to 
//    endorse or promote products derived from this software without specific prior 
//    written permission. 

//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
//  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR 
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
//  WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
//  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
//  POSSIBILITY OF SUCH DAMAGE.

using MEGAxCommon;
using MEGAbolt;


namespace SamplePlugin
{
    public class Extension: IExtension
    {
        // This whole class needs to be implemented in your project to interface
        // with MEGAbolt
        // You obviously need to change the below information to suite your development


        #region IExtension Members
		public IHost Host { get; set; } = null;

        public string Title { get; set; } = "MB Combat Controller";

        public string Description { get; set; } = "This is a plugin to control land combat settings on combat enabled SIMs";

        public string Author { get; set; } = "Legolas Luke";

        public string Version { get; set; } = "V1.0";

        public void Process(MEGAboltInstance instance)
		{ 
			// Show the form to get user input or whatever
            // If you are not using forms then do whatever below
            // or call a process or class etc

            Form1 fi = new Form1(instance);
            fi.Show();
		}

		#endregion
	}
}
