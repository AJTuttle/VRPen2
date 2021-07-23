using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace VRPen {
	public class RemoteMarker : InputVisuals {

		[Header("Remote Marker Params")]
		[Space(10)]
		public Transform snappedTo;
		protected float raycastDistance = 0.2f;
		public Transform modelParent;
		bool snappedToChecker = false;

		public TriggerListener triggers;
		
		protected void Start() {
			
			//set triggers
			triggers.triggerEnter.AddListener(triggerEnter);
			triggers.triggerStay.AddListener(triggerStay);
			triggers.triggerExit.AddListener(triggerExit);
			
			base.Start();
		}

		protected void Update() {
			//base
			base.Update();
		}
		
        private void LateUpdate() {

			if (snappedTo) {

				Vector3 origin = transform.position - snappedTo.forward * raycastDistance;
				RaycastHit[] hits = Physics.RaycastAll(origin, snappedTo.forward, 1f);
				
				//move marker pos;
				foreach(RaycastHit hit in hits) {
					
					Tag tag = hit.collider.GetComponent<Tag>();

					if (tag != null && tag.tag.Equals("Draw_Area")) {

						modelParent.position = hit.point;
						break;

					}

				}

			}

		}

		private void triggerEnter(Collider other) {
			Tag tag;
			if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
				//if (tag.tag.Equals("marker-visible")) visuals.SetActive(true);
				if (tag.tag.Equals("snap")) {
					snappedTo = other.transform;
					snappedToChecker = true;

				}
			}
		}

		private void triggerStay(Collider other) {
			if (snappedTo != null && other.transform == snappedTo) snappedToChecker = true;
		}

		private void triggerExit(Collider other) {

			Tag tag;
			if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
				//if (tag.tag.Equals("marker-visible")) visuals.SetActive(false);
				if (tag.tag.Equals("snap") && snappedTo != null) {
					
					snappedTo = null;
					modelParent.localPosition = Vector3.zero;

				}

			}

		}

		private void FixedUpdate() {
			if (snappedToChecker) {
				snappedToChecker = false;
			}
			else {
				if (snappedTo != null) {
					snappedTo = null;
					modelParent.localPosition = Vector3.zero;
				}
			}
		}
	}
}